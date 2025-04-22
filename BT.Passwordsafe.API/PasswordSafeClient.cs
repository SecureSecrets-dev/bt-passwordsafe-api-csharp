using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BT.PasswordSafe.API.Exceptions;
using BT.PasswordSafe.API.Interfaces;
using BT.PasswordSafe.API.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BT.PasswordSafe.API
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
        private readonly Lazy<Task<AuthenticationResult>> _lazyAuthTask;
        private int _tokenBufferMinutes = 5; // Default buffer time for token expiration
        
        // Add cache for authentication tokens
        private static readonly ConcurrentDictionary<string, AuthenticationResult> _tokenCache = new ConcurrentDictionary<string, AuthenticationResult>();

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

            // Configure the HttpClient for optimal performance
            _httpClient.BaseAddress = new Uri(_options.BaseUrl!);
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            // Set connection pooling settings
            if (_httpClient.DefaultRequestHeaders.Contains("Connection"))
            {
                _httpClient.DefaultRequestHeaders.Remove("Connection");
            }
            _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            
            // Disable Expect: 100-Continue behavior which adds latency
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;
            
            // Set up socket pooling and connection optimization using HttpClientHandler
            if (_httpClient.DefaultRequestHeaders.Contains("SocketsHttpHandler"))
            {
                // Configure connection pooling at the handler level if possible
                // This is a modern approach that doesn't use the obsolete ServicePointManager
                var socketsHandler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                    MaxConnectionsPerServer = 20,
                    EnableMultipleHttp2Connections = true
                };
                
                _logger?.LogInformation("Configured SocketsHttpHandler for optimized connections");
            }
            else
            {
                _logger?.LogInformation("Using default connection settings");
            }
            
            // Initialize lazy authentication
            _lazyAuthTask = new Lazy<Task<AuthenticationResult>>(() => InitializeAuthenticationAsync(CancellationToken.None));
            
            // Set token buffer time from options if specified
            if (_options.TokenBufferMinutes > 0)
            {
                _tokenBufferMinutes = _options.TokenBufferMinutes;
            }
        }

        /// <inheritdoc />
        public async Task<AuthenticationResult> Authenticate(CancellationToken cancellationToken = default)
        {
            // Check cache first before acquiring lock
            if (!string.IsNullOrEmpty(_options.BaseUrl) && 
                _tokenCache.TryGetValue(_options.BaseUrl, out var cachedAuthResult) && 
                !IsTokenExpired(cachedAuthResult))
            {
                _logger?.LogInformation("Using cached authentication token");
                _authResult = cachedAuthResult;
                return _authResult;
            }
            
            // Fast path: return existing valid token without acquiring lock
            if (_authResult != null && !IsTokenExpired(_authResult))
            {
                _logger?.LogInformation("Using existing authentication token without lock");
                return _authResult;
            }

            await _authLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // Check cache again after acquiring lock
                if (!string.IsNullOrEmpty(_options.BaseUrl) && 
                    _tokenCache.TryGetValue(_options.BaseUrl, out cachedAuthResult) && 
                    !IsTokenExpired(cachedAuthResult))
                {
                    _logger?.LogInformation("Using cached authentication token after lock");
                    _authResult = cachedAuthResult;
                    return _authResult;
                }
                
                // Check again after acquiring the lock
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

        private async Task<AuthenticationResult> InitializeAuthenticationAsync(CancellationToken cancellationToken)
        {
            return await Authenticate(cancellationToken).ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AuthenticateWithApiKey(CancellationToken cancellationToken)
        {
            // Remove any existing Authorization header to prevent conflicts
            if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
            }
            
            // Set the API key in the request header
            var authHeader = $"PS-Auth key={_options.ApiKey}; runas={_options.RunAsUsername}";
            
            // Only add password if it's provided
            if (!string.IsNullOrEmpty(_options.RunAsPassword))
            {
                authHeader += $"; pwd=[{_options.RunAsPassword}]";
            }
            
            // Make a simple request to verify the API key works
            using var request = new HttpRequestMessage(HttpMethod.Get, "Auth");
            
            // Optimize request headers
            request.Headers.ConnectionClose = false; // Ensure connection pooling is used
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            request.Headers.Add("Authorization", authHeader);
            
            _logger?.LogInformation("Sending API key authentication request");
            var startTime = DateTimeOffset.UtcNow;
            
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            
            var endTime = DateTimeOffset.UtcNow;
            _logger?.LogInformation($"API key authentication request completed in {(endTime - startTime).TotalMilliseconds}ms with status {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                throw new BeyondTrustAuthenticationException($"API key authentication failed with status code {response.StatusCode}");
            }

            _logger?.LogInformation("Successfully authenticated with API key");

            // Add the authorization header to the default headers for subsequent requests
            _httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);

            // Create a simple AuthenticationResult for API key authentication
            _authResult = new AuthenticationResult
            {
                AccessToken = _options.ApiKey,
                TokenType = "PS-Auth",
                ExpiresIn = 3600, // Default to 1 hour
                IssuedAt = DateTimeOffset.UtcNow
            };

            // Cache the authentication result
            if (!string.IsNullOrEmpty(_options.BaseUrl))
            {
                _tokenCache.TryAdd(_options.BaseUrl, _authResult);
            }

            return _authResult;
        }

        private async Task<AuthenticationResult> AuthenticateWithOAuth(CancellationToken cancellationToken)
        {
            // Remove any existing Authorization header to prevent conflicts
            if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
            }
            
            // Prepare content with StringContent instead of FormUrlEncodedContent for better performance
            var contentString = "grant_type=client_credentials&client_id=" + Uri.EscapeDataString(_options.OAuthClientId!) + 
                               "&client_secret=" + Uri.EscapeDataString(_options.OAuthClientSecret!);
            var content = new StringContent(contentString, Encoding.UTF8, "application/x-www-form-urlencoded");

            // Make the OAuth token request with optimized connection handling
            using var request = new HttpRequestMessage(HttpMethod.Post, "Auth/Connect/Token")
            {
                Content = content
            };
            
            // Optimize request headers
            request.Headers.ConnectionClose = false; // Ensure connection pooling is used
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            request.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            
            _logger?.LogInformation("Sending OAuth authentication request");
            var startTime = DateTimeOffset.UtcNow;
            
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            
            var endTime = DateTimeOffset.UtcNow;
            _logger?.LogInformation($"OAuth request completed in {(endTime - startTime).TotalMilliseconds}ms with status {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                throw new BeyondTrustAuthenticationException($"OAuth authentication failed with status code {response.StatusCode}");
            }

            // Optimize content reading
            var contentStringResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var authResult = JsonConvert.DeserializeObject<AuthenticationResult>(contentStringResponse);

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

            // Prepare the SignAppIn request in parallel with setting up the auth header
            using var signInRequest = new HttpRequestMessage(HttpMethod.Post, "Auth/SignAppIn")
            {
                Content = new StringContent(string.Empty)
            };
            
            // Optimize request headers
            signInRequest.Headers.ConnectionClose = false;
            signInRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            signInRequest.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            signInRequest.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            
            _logger?.LogInformation("Sending SignAppIn request");
            startTime = DateTimeOffset.UtcNow;
            
            var signInResponse = await _httpClient.SendAsync(signInRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            
            endTime = DateTimeOffset.UtcNow;
            _logger?.LogInformation($"SignAppIn request completed in {(endTime - startTime).TotalMilliseconds}ms with status {signInResponse.StatusCode}");

            if (!signInResponse.IsSuccessStatusCode)
            {
                throw new BeyondTrustAuthenticationException($"SignAppIn failed with status code {signInResponse.StatusCode}");
            }

            _logger?.LogInformation("Successfully signed into the app using OAuth token");

            // Cache the authentication result
            if (!string.IsNullOrEmpty(_options.BaseUrl))
            {
                _tokenCache.TryAdd(_options.BaseUrl, _authResult);
            }

            return _authResult;
        }

        private bool IsTokenExpired(AuthenticationResult authResult)
        {
            // Check if the token is expired or about to expire (within configurable buffer time)
            var expiresAt = authResult.IssuedAt.AddSeconds(authResult.ExpiresIn);
            return DateTimeOffset.UtcNow.AddMinutes(_tokenBufferMinutes) >= expiresAt;
        }

        private async Task EnsureAuthenticated(CancellationToken cancellationToken)
        {
            // Use lazy initialization for the first authentication
            if (_authResult == null)
            {
                // Check if there's a cached authentication result
                if (!string.IsNullOrEmpty(_options.BaseUrl) && 
                    _tokenCache.TryGetValue(_options.BaseUrl, out var cachedAuthResult) && 
                    !IsTokenExpired(cachedAuthResult))
                {
                    _authResult = cachedAuthResult;
                    
                    // Ensure the authorization header is set with the cached token
                    ApplyAuthorizationHeader(_authResult);
                    
                    return;
                }

                _authResult = await _lazyAuthTask.Value.ConfigureAwait(false);
                
                // Ensure the authorization header is set
                ApplyAuthorizationHeader(_authResult);
                
                return;
            }
            
            // Check if token needs refresh
            if (IsTokenExpired(_authResult))
            {
                await Authenticate(cancellationToken).ConfigureAwait(false);
                
                // Ensure the authorization header is set after refresh
                ApplyAuthorizationHeader(_authResult);
            }
            else
            {
                // Ensure the authorization header is set even for valid tokens
                ApplyAuthorizationHeader(_authResult);
            }
        }
        
        private void ApplyAuthorizationHeader(AuthenticationResult authResult)
        {
            // Remove any existing Authorization header
            if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
            }
            
            // Apply the appropriate authorization header based on token type
            if (authResult.TokenType == "PS-Auth")
            {
                // For API key authentication
                var authHeader = $"PS-Auth key={_options.ApiKey}; runas={_options.RunAsUsername}";
                
                // Only add password if it's provided
                if (!string.IsNullOrEmpty(_options.RunAsPassword))
                {
                    authHeader += $"; pwd=[{_options.RunAsPassword}]";
                }
                
                _httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);
                _logger?.LogInformation("Applied PS-Auth authorization header");
            }
            else
            {
                // For OAuth authentication
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    authResult.TokenType!,
                    authResult.AccessToken!);
                _logger?.LogInformation("Applied OAuth authorization header");
            }
        }

        /// <inheritdoc />
        public async Task<ManagedPassword> GetManagedAccountPasswordById(string managedAccountId, string? reason = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(managedAccountId))
            {
                throw new ArgumentException("Managed account ID cannot be null or empty", nameof(managedAccountId));
            }

            await EnsureAuthenticated(cancellationToken).ConfigureAwait(false);

            _logger?.LogInformation($"Retrieving managed account password by ID: {managedAccountId}");

            // First get the managed account details
            var account = await GetManagedAccountById(managedAccountId, cancellationToken).ConfigureAwait(false);

            try
            {
                // Create a password request
                var request = new PasswordRequest
                {
                    SystemId = account.ManagedSystemId,
                    AccountId = account.ManagedAccountId,
                    DurationMinutes = _options.DefaultPasswordDuration,
                    Reason = reason,
                    AccessType = "View"
                };

                // Request the password
                var requestResult = await CreatePasswordRequest(request, cancellationToken).ConfigureAwait(false);

                // Get the password using the request ID
                var response = await _httpClient.GetAsync($"Credentials/{requestResult.RequestId}", cancellationToken).ConfigureAwait(false);

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
            catch (BeyondTrustApiException ex) when (ex.Message.Contains("409"))
            {
                // Special handling for 409 Conflict errors
                _logger?.LogWarning($"Conflict detected when retrieving password for account ID: {managedAccountId}. Attempting to find existing request.");
                
                // Try to find an existing active request for this account
                var existingRequest = await GetExistingRequest(account.ManagedAccountId.ToString(), cancellationToken).ConfigureAwait(false);
                
                if (existingRequest != null)
                {
                    _logger?.LogInformation($"Found existing request ID: {existingRequest.RequestId} for account ID: {managedAccountId}. Retrieving password.");
                    
                    // Get the password using the existing request ID
                    var response = await _httpClient.GetAsync($"Credentials/{existingRequest.RequestId}", cancellationToken).ConfigureAwait(false);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new BeyondTrustApiException($"Failed to get password with existing request ID {existingRequest.RequestId}. Status code: {response.StatusCode}");
                    }
                    
                    var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    
                    try
                    {
                        // Try to parse as a complex object first
                        var password = JsonConvert.DeserializeObject<ManagedPassword>(content);
                        if (password != null)
                        {
                            // Set additional properties from the request
                            password.RequestId = existingRequest.RequestId;
                            password.AccountId = account.ManagedAccountId;
                            password.SystemId = account.ManagedSystemId;
                            password.ExpirationDate = existingRequest.ExpirationDate;
                            
                            return password;
                        }
                        
                        // If complex object deserialization doesn't return a valid object, try parsing as a simple string
                        var rawPassword = content.Trim('"');
                        return new ManagedPassword
                        {
                            Password = rawPassword,
                            RequestId = existingRequest.RequestId,
                            AccountId = account.ManagedAccountId,
                            SystemId = account.ManagedSystemId,
                            ExpirationDate = existingRequest.ExpirationDate
                        };
                    }
                    catch (Exception parseEx)
                    {
                        throw new BeyondTrustApiException($"Failed to parse password response from existing request: {content}", parseEx);
                    }
                }
                
                // If we couldn't find an existing request, rethrow the original exception
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<ManagedPassword> GetManagedAccountPasswordByName(string accountName, string? systemName = null, string? domainName = null, bool isDomainLinked = false, string? reason = null, CancellationToken cancellationToken = default)
        {
            // Get the managed account details first
            var account = await GetManagedAccountByName(accountName, systemName, domainName, isDomainLinked, cancellationToken).ConfigureAwait(false);

            // Now that we have the account ID, get the password
            return await GetManagedAccountPasswordById(account.ManagedAccountId.ToString(), reason, cancellationToken).ConfigureAwait(false);
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
        public async Task<IEnumerable<ManagedAccount>> GetManagedAccounts(string? systemId = null, string? accountName = null, CancellationToken cancellationToken = default)
        {
            await EnsureAuthenticated(cancellationToken).ConfigureAwait(false);

            string endpoint;
            
            if (string.IsNullOrEmpty(systemId))
            {
                endpoint = "ManagedAccounts";
            }
            else if (string.IsNullOrEmpty(accountName))
            {
                endpoint = $"ManagedSystems/{systemId}/ManagedAccounts";
            }
            else
            {
                // Use the specific API endpoint for getting a managed account by system ID and account name
                endpoint = $"ManagedSystems/{systemId}/ManagedAccounts?name={Uri.EscapeDataString(accountName)}";
                _logger?.LogInformation($"Getting managed account by system ID: {systemId} and account name: {accountName}");
            }

            _logger?.LogInformation($"Getting managed accounts from endpoint: {endpoint}");

            var response = await _httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new BeyondTrustApiException($"Failed to get managed accounts with status code {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            
            // If we're querying by account name, the API returns a single object, not an array
            if (!string.IsNullOrEmpty(systemId) && !string.IsNullOrEmpty(accountName))
            {
                try
                {
                    var account = JsonConvert.DeserializeObject<ManagedAccount>(content);
                    
                    if (account == null)
                    {
                        throw new BeyondTrustApiException("Failed to deserialize managed account response");
                    }
                    
                    return new List<ManagedAccount> { account };
                }
                catch (JsonException)
                {
                    // If it fails to deserialize as a single object, try as an array
                    var accounts = JsonConvert.DeserializeObject<List<ManagedAccount>>(content);
                    
                    if (accounts == null)
                    {
                        throw new BeyondTrustApiException("Failed to deserialize managed accounts response");
                    }
                    
                    return accounts;
                }
            }
            else
            {
                var accounts = JsonConvert.DeserializeObject<List<ManagedAccount>>(content);

                if (accounts == null)
                {
                    throw new BeyondTrustApiException("Failed to deserialize managed accounts response");
                }

                return accounts;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ManagedSystem>> GetManagedSystems(string? systemId = null, CancellationToken cancellationToken = default)
        {
            await EnsureAuthenticated(cancellationToken).ConfigureAwait(false);

            string endpoint = string.IsNullOrEmpty(systemId)
                ? "ManagedSystems"
                : $"ManagedSystems/{systemId}";

            _logger?.LogInformation(string.IsNullOrEmpty(systemId)
                ? "Getting all managed systems"
                : $"Getting managed system by ID: {systemId}");

            var response = await _httpClient.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new BeyondTrustApiException($"Failed to get managed systems with status code {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            // If we're querying by system ID, the API returns a single object, not an array
            if (!string.IsNullOrEmpty(systemId))
            {
                try
                {
                    var system = JsonConvert.DeserializeObject<ManagedSystem>(content);
                    
                    if (system == null)
                    {
                        throw new BeyondTrustApiException("Failed to deserialize managed system response");
                    }
                    
                    return new List<ManagedSystem> { system };
                }
                catch (JsonException)
                {
                    // If it fails to deserialize as a single object, try as an array
                    var systems = JsonConvert.DeserializeObject<List<ManagedSystem>>(content);
                    
                    if (systems == null)
                    {
                        throw new BeyondTrustApiException("Failed to deserialize managed systems response");
                    }
                    
                    return systems;
                }
            }
            else
            {
                var systems = JsonConvert.DeserializeObject<List<ManagedSystem>>(content);

                if (systems == null)
                {
                    throw new BeyondTrustApiException("Failed to deserialize managed systems response");
                }

                return systems;
            }
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

            // If we get a 409 Conflict, it likely means there's already an active request for this account
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger?.LogInformation($"Conflict detected when creating password request for account ID: {request.AccountId}. Checking for existing requests.");
                
                // Try to find an existing active request for this account
                var existingRequest = await GetExistingRequest(request.AccountId.ToString(), cancellationToken).ConfigureAwait(false);
                
                if (existingRequest != null)
                {
                    _logger?.LogInformation($"Found existing request ID: {existingRequest.RequestId} for account ID: {request.AccountId}");
                    return existingRequest;
                }
                
                // If we couldn't find an existing request, throw the original exception
                throw new BeyondTrustApiException($"Failed to create password request with status code {response.StatusCode} and no existing request was found");
            }
            else if (!response.IsSuccessStatusCode)
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

        /// <summary>
        /// Gets an existing active request for the specified account ID
        /// </summary>
        /// <param name="accountId">The account ID to check for existing requests</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The existing request if found, null otherwise</returns>
        private async Task<PasswordRequestResult?> GetExistingRequest(string accountId, CancellationToken cancellationToken = default)
        {
            await EnsureAuthenticated(cancellationToken).ConfigureAwait(false);

            _logger?.LogInformation($"Checking for existing requests for account ID: {accountId}");

            // Get all active requests for the current user
            var response = await _httpClient.GetAsync("Requests?status=active&queue=req", cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogWarning($"Failed to get existing requests with status code {response.StatusCode}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            
            try
            {
                var requests = JsonConvert.DeserializeObject<List<dynamic>>(content);
                
                if (requests == null || requests.Count == 0)
                {
                    _logger?.LogInformation("No existing requests found");
                    return null;
                }

                // Find a request for the specified account ID
                foreach (var req in requests)
                {
                    if (req.AccountID != null && req.AccountID.ToString() == accountId)
                    {
                        _logger?.LogInformation($"Found existing request ID: {req.RequestID} for account ID: {accountId}");
                        
                        // Create a PasswordRequestResult from the dynamic object
                        var result = new PasswordRequestResult
                        {
                            RequestId = req.RequestID.ToString(),
                            Status = req.Status.ToString(),
                            SystemId = req.SystemID != null ? Convert.ToInt32(req.SystemID) : 0,
                            AccountId = Convert.ToInt32(accountId),
                            CreatedDate = req.ApprovedDate != null ? 
                                DateTimeOffset.Parse(req.ApprovedDate.ToString()) : 
                                DateTimeOffset.UtcNow,
                            ExpirationDate = req.ExpiresDate != null ? 
                                DateTimeOffset.Parse(req.ExpiresDate.ToString()) : 
                                DateTimeOffset.UtcNow.AddMinutes(_options.DefaultPasswordDuration)
                        };
                        
                        return result;
                    }
                }
                
                _logger?.LogInformation($"No existing requests found for account ID: {accountId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Error parsing existing requests: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<ManagedPassword> GetManagedAccountPasswordByRequestId(string requestId, string? reason = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(requestId))
            {
                throw new ArgumentException("Request ID cannot be null or empty", nameof(requestId));
            }

            await EnsureAuthenticated(cancellationToken).ConfigureAwait(false);

            _logger?.LogInformation($"Retrieving managed account password by request ID: {requestId}");

            // Get the password using the request ID
            var response = await _httpClient.GetAsync($"Credentials/{requestId}", cancellationToken).ConfigureAwait(false);

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
                    password.RequestId = requestId;
                    
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
                        RequestId = requestId
                    };

                    return password;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to parse password response");
                    throw new BeyondTrustApiException("Failed to parse password response", ex);
                }
            }

            throw new BeyondTrustApiException("Failed to retrieve password");
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
                    Reason = reason ?? "API Check-in"
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
        /// Gets a secret by its ID.
        /// </summary>
        /// <param name="secretId">The ID of the secret</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The secret if found, otherwise null</returns>
        public async Task<SecretSafe?> GetSecretById(Guid secretId, CancellationToken cancellationToken = default)
        {
            await EnsureAuthenticated(cancellationToken).ConfigureAwait(false);
            _logger?.LogInformation($"Retrieving secret by ID: {secretId}");
            var response = await _httpClient.GetAsync($"Secrets-Safe/Secrets/{secretId}", cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            if (!response.IsSuccessStatusCode)
                throw new BeyondTrustApiException($"Failed to retrieve secret by ID with status code {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<SecretSafe>(content);
        }

        /// <summary>
        /// Gets a secret by its name (title).
        /// </summary>
        /// <param name="name">The title of the secret</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The secret if found, otherwise null</returns>
        public async Task<SecretSafe?> GetSecretByName(string name, CancellationToken cancellationToken = default)
        {
            await EnsureAuthenticated(cancellationToken).ConfigureAwait(false);
            _logger?.LogInformation($"Retrieving secret by name: {name}");
            var response = await _httpClient.GetAsync($"Secrets-Safe/Secrets?Title={Uri.EscapeDataString(name)}", cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new BeyondTrustApiException($"Failed to retrieve secret by name with status code {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var secrets = JsonConvert.DeserializeObject<List<SecretSafe>>(content);
            return secrets?.Count > 0 ? secrets[0] : null;
        }

        /// <summary>
        /// Tests the current credentials of a managed account by ID.
        /// </summary>
        /// <param name="managedAccountId">ID of the managed account</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the credential test succeeded, otherwise false</returns>
        public async Task<bool> TestCredentialByAccountID(string managedAccountId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(managedAccountId))
            {
                throw new ArgumentException("Managed account ID cannot be null or empty", nameof(managedAccountId));
            }

            await EnsureAuthenticated(cancellationToken).ConfigureAwait(false);

            _logger?.LogInformation($"Testing credentials for managed account ID: {managedAccountId}");

            var response = await _httpClient.PostAsync($"ManagedAccounts/{managedAccountId}/Credentials/Test", null, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new BeyondTrustApiException($"Failed to test credentials with status code {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = JsonConvert.DeserializeObject<CredentialTestResult>(content);

            if (result == null)
            {
                throw new BeyondTrustApiException("Failed to deserialize credential test response");
            }

            return result.Success;
        }

        /// <summary>
        /// Tests the current credentials of a managed account by name.
        /// </summary>
        /// <param name="accountName">Name of the managed account</param>
        /// <param name="systemName">Name of the managed system (required if isDomainLinked is false)</param>
        /// <param name="domainName">Name of the domain (required if isDomainLinked is true)</param>
        /// <param name="isDomainLinked">Whether the account is domain-linked (true) or local (false)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the credential test succeeded, otherwise false</returns>
        public async Task<bool> TestCredentialByAccountName(string accountName, string? systemName = null, string? domainName = null, bool isDomainLinked = false, CancellationToken cancellationToken = default)
        {
            // Get the managed account details first
            var account = await GetManagedAccountByName(accountName, systemName, domainName, isDomainLinked, cancellationToken).ConfigureAwait(false);

            // Now that we have the account ID, test the credentials
            return await TestCredentialByAccountID(account.ManagedAccountId.ToString(), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Changes the current credentials of a managed account by ID.
        /// </summary>
        /// <param name="managedAccountId">ID of the managed account</param>
        /// <param name="queue">True to queue the change for background processing, otherwise false</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task ChangeCredentialByAccountID(string managedAccountId, bool queue = false, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(managedAccountId))
            {
                throw new ArgumentException("Managed account ID cannot be null or empty", nameof(managedAccountId));
            }

            await EnsureAuthenticated(cancellationToken).ConfigureAwait(false);

            _logger?.LogInformation($"Changing credentials for managed account ID: {managedAccountId}");

            // Create request body if queue parameter is provided
            HttpContent? content = null;
            if (queue)
            {
                var requestBody = new { Queue = true };
                content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.PostAsync($"ManagedAccounts/{managedAccountId}/Credentials/Change", content, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new BeyondTrustApiException($"Failed to change credentials with status code {response.StatusCode}");
            }

            // No content in response body for successful request
        }

        /// <summary>
        /// Changes the current credentials of a managed account by name.
        /// </summary>
        /// <param name="accountName">Name of the managed account</param>
        /// <param name="systemName">Name of the managed system (required if isDomainLinked is false)</param>
        /// <param name="domainName">Name of the domain (required if isDomainLinked is true)</param>
        /// <param name="isDomainLinked">Whether the account is domain-linked (true) or local (false)</param>
        /// <param name="queue">True to queue the change for background processing, otherwise false</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the asynchronous operation</returns>
        public async Task ChangeCredentialByAccountName(string accountName, string? systemName = null, string? domainName = null, bool isDomainLinked = false, bool queue = false, CancellationToken cancellationToken = default)
        {
            // Get the managed account details first
            var account = await GetManagedAccountByName(accountName, systemName, domainName, isDomainLinked, cancellationToken).ConfigureAwait(false);

            // Now that we have the account ID, change the credentials
            await ChangeCredentialByAccountID(account.ManagedAccountId.ToString(), queue, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Preloads the authentication token in the background to avoid delay during the first API call
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public Task PreloadAuthentication(CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Preloading authentication token in background");
            
            // Start authentication in the background
            Task.Run(async () => 
            {
                try 
                {
                    await Authenticate(cancellationToken).ConfigureAwait(false);
                    _logger?.LogInformation("Authentication token preloaded successfully");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to preload authentication token");
                }
            }, cancellationToken);
            
            // Don't wait for completion - let it run in background
            return Task.CompletedTask;
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
