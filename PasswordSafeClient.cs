using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PRK.BT.PasswordSafe.SDK.Exceptions;
using PRK.BT.PasswordSafe.SDK.Interfaces;
using PRK.BT.PasswordSafe.SDK.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace PRK.BT.PasswordSafe.SDK
{
    /// <summary>
    /// Client for interacting with the Password Safe API
    /// </summary>
    public class PasswordSafeClient : IPasswordSafeClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly PasswordSafeOptions _options;
        private readonly ILogger<PasswordSafeClient>? _logger;
        private AuthenticationResult? _authResult;
        private bool _disposed;
        private readonly SemaphoreSlim _authLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordSafeClient"/> class
        /// </summary>
        /// <param name="httpClient">The HTTP client</param>
        /// <param name="options">The Password Safe options</param>
        /// <param name="logger">The logger</param>
        public PasswordSafeClient(
            HttpClient httpClient,
            IOptions<PasswordSafeOptions> options,
            ILogger<PasswordSafeClient>? logger = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;

            _options.Validate();

            // Configure the HttpClient
            _httpClient.BaseAddress = new Uri(_options.BaseUrl!);
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <inheritdoc />
        public async Task<AuthenticationResult> Authenticate(CancellationToken cancellationToken = default)
        {
            await _authLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // Check if we already have a valid token
                if (_authResult != null && !IsTokenExpired(_authResult))
                {
                    _logger?.LogInformation("Using existing authentication token");
                    return _authResult;
                }

                _logger?.LogInformation("Authenticating with Password Safe API");

                // Choose the authentication method based on the options
                if (_options.UseOAuth)
                {
                    return await AuthenticateWithOAuth(cancellationToken).ConfigureAwait(false);
                }

                // Fall back to API key authentication
                return await AuthenticateWithApiKey(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _authLock.Release();
            }
        }

        private async Task<AuthenticationResult> AuthenticateWithApiKey(CancellationToken cancellationToken)
        {
            // Set the API key in the request header
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"PS-Auth key={_options.ApiKey}; runas={_options.RunAsUsername}; pwd={_options.RunAsPassword};");

            // Make a simple request to verify the API key works
            var response = await _httpClient.GetAsync("Auth", cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new BeyondTrustAuthenticationException($"API key authentication failed with status code {response.StatusCode}");
            }

            _logger?.LogInformation("Successfully authenticated with API key");

            // Create a simple AuthenticationResult for API key authentication
            _authResult = new AuthenticationResult
            {
                AccessToken = _options.ApiKey,
                TokenType = "PS-Auth",
                ExpiresIn = 3600, // Default to 1 hour
                IssuedAt = DateTimeOffset.UtcNow
            };

            return _authResult;
        }

        private async Task<AuthenticationResult> AuthenticateWithOAuth(CancellationToken cancellationToken)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _options.OAuthClientId!),
                new KeyValuePair<string, string>("client_secret", _options.OAuthClientSecret!)
            });

            // Make the OAuth token request
            var response = await _httpClient.PostAsync("Auth/Connect/Token", formContent, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new BeyondTrustAuthenticationException($"OAuth authentication failed with status code {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var authResult = JsonConvert.DeserializeObject<AuthenticationResult>(content);

            if (authResult == null)
            {
                throw new BeyondTrustAuthenticationException("Failed to deserialize authentication response");
            }

            _authResult = authResult;
            _authResult.IssuedAt = DateTimeOffset.UtcNow;

            // Set the authorization header for subsequent requests
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                _authResult.TokenType!,
                _authResult.AccessToken!);

            // Additional step: Sign into the app using the obtained access token
            var signInResponse = await _httpClient.PostAsync("Auth/SignAppIn", new StringContent(string.Empty), cancellationToken).ConfigureAwait(false);

            if (!signInResponse.IsSuccessStatusCode)
            {
                throw new BeyondTrustAuthenticationException($"SignAppIn failed with status code {signInResponse.StatusCode}");
            }

            _logger?.LogInformation("Successfully signed into the app using OAuth token");

            return _authResult;
        }

        private bool IsTokenExpired(AuthenticationResult authResult)
        {
            // Check if the token is expired or about to expire (within 5 minutes)
            var expiresAt = authResult.IssuedAt.AddSeconds(authResult.ExpiresIn);
            return DateTimeOffset.UtcNow.AddMinutes(5) >= expiresAt;
        }

        private async Task EnsureAuthenticated(CancellationToken cancellationToken)
        {
            if (_authResult == null || IsTokenExpired(_authResult))
            {
                await Authenticate(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<ManagedPassword> GetManagedAccountPasswordById(string managedAccountId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(managedAccountId))
            {
                throw new ArgumentException("Managed account ID cannot be null or empty", nameof(managedAccountId));
            }

            await EnsureAuthenticated(cancellationToken).ConfigureAwait(false);

            _logger?.LogInformation($"Retrieving managed account password by ID: {managedAccountId}");

            // First get the managed account details
            var account = await GetManagedAccountById(managedAccountId, cancellationToken).ConfigureAwait(false);

            // Create a password request
            var request = new PasswordRequest
            {
                SystemId = account.ManagedSystemId,
                AccountId = account.ManagedAccountId,
                DurationMinutes = _options.DefaultPasswordDuration,
                Reason = "SDK Password Request"
            };

            // Request the password
            var requestResult = await CreatePasswordRequest(request, cancellationToken).ConfigureAwait(false);

            // Get the password using the request ID
            var response = await _httpClient.GetAsync($"Requests/{requestResult.RequestId}/Password", cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new BeyondTrustApiException($"Failed to get password with status code {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            ManagedPassword? password = null;

            try
            {
                // First try to parse as a complex object
                password = JsonConvert.DeserializeObject<ManagedPassword>(content);
                if (password != null)
                {
                    // Set additional properties from the request
                    password.RequestId = requestResult.RequestId;
                    password.AccountId = account.ManagedAccountId;
                    password.SystemId = account.ManagedSystemId;
                    password.ExpirationDate = requestResult.ExpirationDate;

                    return password;
                }
            }
            catch (JsonException)
            {
                // If complex object deserialization fails, try parsing as a simple password string
                try
                {
                    // The API might just return the password as a plain string
                    var rawPassword = content.Trim('"');
                    password = new ManagedPassword
                    {
                        Password = rawPassword,
                        RequestId = requestResult.RequestId,
                        AccountId = account.ManagedAccountId,
                        SystemId = account.ManagedSystemId,
                        ExpirationDate = requestResult.ExpirationDate
                    };

                    return password;
                }
                catch
                {
                    // If all parsing attempts fail, throw an exception
                    throw new BeyondTrustApiException($"Failed to parse password response: {content}");
                }
            }

            throw new BeyondTrustApiException($"Failed to parse password response: {content}");
        }

        /// <inheritdoc />
        public async Task<ManagedPassword> GetManagedAccountPasswordByName(string accountName, string? systemName = null, string? domainName = null, bool isDomainLinked = false, CancellationToken cancellationToken = default)
        {
            // Get the managed account details first
            var account = await GetManagedAccountByName(accountName, systemName, domainName, isDomainLinked, cancellationToken).ConfigureAwait(false);

            // Now that we have the account ID, get the password
            return await GetManagedAccountPasswordById(account.ManagedAccountId.ToString(), cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ManagedAccount> GetManagedAccountByName(string accountName, string? systemName = null, string? domainName = null, bool isDomainLinked = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(accountName))
            {
                throw new ArgumentException("Account name cannot be null or empty", nameof(accountName));
            }

            if (isDomainLinked)
            {
                if (string.IsNullOrEmpty(domainName))
                {
                    throw new ArgumentException("Domain name is required when isDomainLinked is true", nameof(domainName));
                }
            }
            else
            {
                if (string.IsNullOrEmpty(systemName))
                {
                    throw new ArgumentException("System name is required when isDomainLinked is false", nameof(systemName));
                }
            }

            await EnsureAuthenticated(cancellationToken).ConfigureAwait(false);

            _logger?.LogInformation($"Retrieving managed account by name: {accountName}");

            // Build the query parameters
            var queryParams = new List<string>();

            if (isDomainLinked)
            {
                // For domain-linked accounts, format is accountname=domain\accountName
                queryParams.Add($"accountname={Uri.EscapeDataString($"{domainName}\\{accountName}")}");
                queryParams.Add("type=domainlinked");
            }
            else
            {
                // For local accounts, use systemName and accountName separately
                queryParams.Add($"systemName={Uri.EscapeDataString(systemName!)}");
                queryParams.Add($"accountName={Uri.EscapeDataString(accountName)}");
            }

            // Construct the query string
            var queryString = string.Join("&", queryParams);

            // Get the managed account details
            var response = await _httpClient.GetAsync($"ManagedAccounts?{queryString}", cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new BeyondTrustApiException($"Failed to retrieve managed account with status code {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            ManagedAccount account;

            try
            {
                // The API returns a single object when both systemName and accountName are provided
                // or when a domain account is specified with domain\accountName format
                var deserializedAccount = JsonConvert.DeserializeObject<ManagedAccount>(content);

                if (deserializedAccount == null)
                {
                    throw new BeyondTrustApiException("Failed to deserialize account");
                }

                account = deserializedAccount;

                // If AccountId is populated but ManagedAccountId is not, copy the value
                if (account.AccountId > 0 && account.ManagedAccountId == 0)
                {
                    account.ManagedAccountId = account.AccountId;
                }

                // If SystemId is populated but ManagedSystemId is not, copy the value
                if (account.SystemId > 0 && account.ManagedSystemId == 0)
                {
                    account.ManagedSystemId = account.SystemId;
                }
            }
            catch (JsonException)
            {
                // If we got an array instead (possible in some cases), try to deserialize as array and take first item
                try
                {
                    var accounts = JsonConvert.DeserializeObject<List<ManagedAccount>>(content);
                    if (accounts == null || accounts.Count == 0)
                    {
                        throw new BeyondTrustApiException($"No managed accounts found matching the criteria");
                    }

                    account = accounts[0];
                }
                catch (Exception ex) when (!(ex is BeyondTrustApiException))
                {
                    throw new BeyondTrustApiException($"Failed to parse managed account response: {content}", ex);
                }
            }

            return account;
        }

        private async Task<ManagedAccount> GetManagedAccountById(string managedAccountId, CancellationToken cancellationToken)
        {
            await EnsureAuthenticated(cancellationToken).ConfigureAwait(false);

            _logger?.LogInformation($"Retrieving managed account by ID: {managedAccountId}");

            var response = await _httpClient.GetAsync($"ManagedAccounts/{managedAccountId}", cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new BeyondTrustApiException($"Failed to retrieve managed account with status code {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var account = JsonConvert.DeserializeObject<ManagedAccount>(content);

            if (account == null)
            {
                throw new BeyondTrustApiException("Failed to deserialize managed account response");
            }

            return account;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ManagedAccount>> GetManagedAccounts(string? systemId = null, CancellationToken cancellationToken = default)
        {
            await EnsureAuthenticated(cancellationToken).ConfigureAwait(false);

            string endpoint = string.IsNullOrEmpty(systemId)
                ? "ManagedAccounts"
                : $"ManagedSystems/{systemId}/ManagedAccounts";

            _logger?.LogInformation($"Getting managed accounts from endpoint: {endpoint}");

            var response = await _httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new BeyondTrustApiException($"Failed to get managed accounts with status code {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var accounts = JsonConvert.DeserializeObject<List<ManagedAccount>>(content);

            if (accounts == null)
            {
                throw new BeyondTrustApiException("Failed to deserialize managed accounts response");
            }

            return accounts;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ManagedSystem>> GetManagedSystems(CancellationToken cancellationToken = default)
        {
            await EnsureAuthenticated(cancellationToken).ConfigureAwait(false);

            _logger?.LogInformation("Getting managed systems");

            var response = await _httpClient.GetAsync("ManagedSystems", cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new BeyondTrustApiException($"Failed to get managed systems with status code {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var systems = JsonConvert.DeserializeObject<List<ManagedSystem>>(content);

            if (systems == null)
            {
                throw new BeyondTrustApiException("Failed to deserialize managed systems response");
            }

            return systems;
        }

        /// <inheritdoc />
        public async Task<PasswordRequestResult> CreatePasswordRequest(PasswordRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await EnsureAuthenticated(cancellationToken).ConfigureAwait(false);

            _logger?.LogInformation($"Creating password request for account ID: {request.AccountId}");

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("Requests", content, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new BeyondTrustApiException($"Failed to create password request with status code {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            try
            {
                // First try to parse as a complex object
                var result = JsonConvert.DeserializeObject<PasswordRequestResult>(responseContent);
                if (result != null)
                {
                    return result;
                }
            }
            catch
            {
                // If complex object deserialization fails, try parsing as a simple request ID
                int requestId = 0;
                long longRequestId = 0;

                if (int.TryParse(responseContent, out requestId) ||
                    long.TryParse(responseContent, out longRequestId))
                {
                    // Create a PasswordRequestResult with the request ID
                    var result = new PasswordRequestResult
                    {
                        RequestId = responseContent,
                        Status = "Approved", // Assume approved if we got an ID back
                        CreatedDate = DateTimeOffset.UtcNow,
                        ExpirationDate = DateTimeOffset.UtcNow.AddMinutes(request.DurationMinutes),
                        SystemId = request.SystemId,
                        AccountId = request.AccountId
                    };

                    return result;
                }
            }

            throw new BeyondTrustApiException($"Failed to parse password request response: {responseContent}");
        }

        /// <inheritdoc />
        public async Task<bool> CheckInPassword(string requestId, string? reason = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(requestId))
            {
                throw new ArgumentException("Request ID cannot be null or empty", nameof(requestId));
            }

            await EnsureAuthenticated(cancellationToken).ConfigureAwait(false);

            _logger?.LogInformation($"Checking in password for request ID: {requestId}");

            try
            {
                // Create a proper request body with the user-provided reason (or default if not provided)
                var requestBody = new
                {
                    Reason = reason ?? "SDK Check-in"
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"Requests/{requestId}/Checkin", content, cancellationToken).ConfigureAwait(false);

                // 204 No Content is the success response for check-in
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent || response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation($"Successfully checked in password for request ID: {requestId}");
                    return true;
                }

                // If we get here, the request was not successful
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new BeyondTrustApiException($"Failed to check in password with status code {response.StatusCode}. Details: {errorContent}");
            }
            catch (HttpRequestException ex)
            {
                throw new BeyondTrustApiException($"HTTP error occurred while checking in password: {ex.Message}", ex);
            }
        }

        /// <inheritdoc />
        public async Task<bool> SignOut(CancellationToken cancellationToken = default)
        {
            try
            {
                // No need to sign out if we're not authenticated
                if (_authResult == null)
                {
                    return true;
                }

                _logger?.LogInformation("Signing out from Password Safe");

                // Make the sign-out request
                var response = await _httpClient.PostAsync("Auth/Signout", new StringContent(string.Empty), cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    throw new BeyondTrustApiException($"Sign-out failed with status code {response.StatusCode}");
                }

                // Clear the authentication result
                _authResult = null;

                _logger?.LogInformation("Successfully signed out from Password Safe");
                return true;
            }
            catch (HttpRequestException ex)
            {
                throw new BeyondTrustApiException($"HTTP error occurred while signing out: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Disposes the client resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the client resources
        /// </summary>
        /// <param name="disposing">Whether to dispose managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed resources
                _authLock.Dispose();
            }

            _disposed = true;
        }
    }
}
