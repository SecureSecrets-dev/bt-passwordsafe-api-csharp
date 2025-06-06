using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BT.PasswordSafe.API.Models;

namespace BT.PasswordSafe.API.Interfaces
{
    /// <summary>
    /// Interface for the Password Safe client
    /// </summary>
    public interface IPasswordSafeClient
    {
        /// <summary>
        /// Authenticates with the Password Safe API
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Authentication result</returns>
        Task<AuthenticationResult> Authenticate(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a managed password by account ID
        /// </summary>
        /// <param name="managedAccountId">The ID of the managed account</param>
        /// <param name="reason">Optional reason for retrieving the password</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Managed password</returns>
        Task<ManagedPassword> GetManagedAccountPasswordById(string managedAccountId, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a managed password by account name
        /// </summary>
        /// <param name="accountName">Name of the managed account</param>
        /// <param name="systemName">Name of the managed system (required if isDomainLinked is false)</param>
        /// <param name="domainName">Name of the domain (required if isDomainLinked is true)</param>
        /// <param name="isDomainLinked">Whether the account is domain-linked (true) or local (false)</param>
        /// <param name="reason">Optional reason for retrieving the password</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Managed password</returns>
        Task<ManagedPassword> GetManagedAccountPasswordByName(string accountName, string? systemName = null, string? domainName = null, bool isDomainLinked = false, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a managed account by name
        /// </summary>
        /// <param name="accountName">Name of the managed account</param>
        /// <param name="systemName">Name of the managed system (required if isDomainLinked is false)</param>
        /// <param name="domainName">Name of the domain (required if isDomainLinked is true)</param>
        /// <param name="isDomainLinked">Whether the account is domain-linked (true) or local (false)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Managed account</returns>
        Task<ManagedAccount> GetManagedAccountByName(string accountName, string? systemName = null, string? domainName = null, bool isDomainLinked = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of managed accounts
        /// </summary>
        /// <param name="systemId">Optional system ID filter</param>
        /// <param name="accountName">Optional account name filter (requires systemId to be specified)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of managed accounts</returns>
        Task<IEnumerable<ManagedAccount>> GetManagedAccounts(string? systemId = null, string? accountName = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of managed systems
        /// </summary>
        /// <param name="systemId">Optional system ID to retrieve a specific managed system</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of managed systems</returns>
        Task<IEnumerable<ManagedSystem>> GetManagedSystems(string? systemId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a managed password by request ID
        /// </summary>
        /// <param name="requestId">The ID of the password request</param>
        /// <param name="reason">Optional reason for retrieving the password</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Managed password</returns>
        Task<ManagedPassword> GetManagedAccountPasswordByRequestId(string requestId, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a password request
        /// </summary>
        /// <param name="request">The password request details</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Password request result</returns>
        Task<PasswordRequestResult> CreatePasswordRequest(PasswordRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks in a password that was previously checked out
        /// </summary>
        /// <param name="requestId">The ID of the password request</param>
        /// <param name="reason">Optional reason for checking in the password</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful</returns>
        Task<bool> CheckInPassword(string requestId, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Signs out the current user session
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if successful</returns>
        Task<bool> SignOut(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a secret by its ID
        /// </summary>
        /// <param name="secretId">The ID of the secret</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The secret if found, otherwise null</returns>
        Task<SecretSafe?> GetSecretById(Guid secretId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a secret by its name (title)
        /// </summary>
        /// <param name="name">The title of the secret</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The secret if found, otherwise null</returns>
        Task<SecretSafe?> GetSecretByName(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests the current credentials of a managed account by ID
        /// </summary>
        /// <param name="managedAccountId">ID of the managed account</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the credential test succeeded, otherwise false</returns>
        Task<bool> TestCredentialByAccountID(string managedAccountId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests the current credentials of a managed account by name
        /// </summary>
        /// <param name="accountName">Name of the managed account</param>
        /// <param name="systemName">Name of the managed system (required if isDomainLinked is false)</param>
        /// <param name="domainName">Name of the domain (required if isDomainLinked is true)</param>
        /// <param name="isDomainLinked">Whether the account is domain-linked (true) or local (false)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the credential test succeeded, otherwise false</returns>
        Task<bool> TestCredentialByAccountName(string accountName, string? systemName = null, string? domainName = null, bool isDomainLinked = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Changes the current credentials of a managed account by ID
        /// </summary>
        /// <param name="managedAccountId">ID of the managed account</param>
        /// <param name="queue">True to queue the change for background processing, otherwise false</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task ChangeCredentialByAccountID(string managedAccountId, bool queue = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Changes the current credentials of a managed account by name
        /// </summary>
        /// <param name="accountName">Name of the managed account</param>
        /// <param name="systemName">Name of the managed system (required if isDomainLinked is false)</param>
        /// <param name="domainName">Name of the domain (required if isDomainLinked is true)</param>
        /// <param name="isDomainLinked">Whether the account is domain-linked (true) or local (false)</param>
        /// <param name="queue">True to queue the change for background processing, otherwise false</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task ChangeCredentialByAccountName(string accountName, string? systemName = null, string? domainName = null, bool isDomainLinked = false, bool queue = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Preloads the authentication token in the background to avoid delay during the first API call
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A task representing the asynchronous operation</returns>
        Task PreloadAuthentication(CancellationToken cancellationToken = default);
    }
}
