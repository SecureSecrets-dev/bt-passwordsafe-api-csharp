# PRK.BT.PasswordSafe.SDK

A modern .NET SDK for interacting with BeyondTrust Password Safe API. This SDK provides a simple and intuitive interface for retrieving and managing passwords from BeyondTrust Password Safe.

[![NuGet](https://img.shields.io/nuget/v/PRK.BT.PasswordSafe.SDK.svg)](https://www.nuget.org/packages/PRK.BT.PasswordSafe.SDK/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Features

- 🔐 **Secure Authentication**: Support for both API Key and OAuth authentication methods
- 🔄 **Automatic Token Management**: Handles token refresh and expiration automatically
- 🔍 **Account Discovery**: Find managed accounts by ID or name
- 🔑 **Password Retrieval**: Get passwords for managed accounts with automatic request handling
- 🧩 **Conflict Resolution**: Intelligently handles existing password requests (409 Conflict)
- 📝 **Comprehensive Logging**: Detailed logging for troubleshooting
- 🧰 **Dependency Injection**: Designed to work with Microsoft's dependency injection
- ⚡ **Async Support**: Full async/await support for all operations

## Installation

```bash
dotnet add package PRK.BT.PasswordSafe.SDK
```

## Quick Start

### Register Services

```csharp
// Add to your service collection
services.AddPasswordSafeClient(options =>
{
    options.BaseUrl = "https://your-instance.beyondtrustcloud.com/BeyondTrust/api/public/v3/";
    
    // API Key Authentication
    options.ApiKey = "your-api-key";
    options.RunAsUsername = "your-username";
    options.RunAsPassword = "your-password";
    
    // Or OAuth Authentication
    options.UseOAuth = true;
    options.OAuthClientId = "your-client-id";
    options.OAuthClientSecret = "your-client-secret";
    
    // Other options
    options.TimeoutSeconds = 30;
    options.DefaultPasswordDuration = 60; // minutes
    options.AutoRefreshToken = true;
});
```

### Retrieve a Password

```csharp
public class PasswordService
{
    private readonly IPasswordSafeClient _client;
    
    public PasswordService(IPasswordSafeClient client)
    {
        _client = client;
    }
    
    public async Task<string> GetPasswordByAccountId(string accountId)
    {
        var password = await _client.GetManagedAccountPasswordById(accountId);
        return password.Password;
    }
    
    public async Task<string> GetPasswordByAccountName(string accountName, string systemName, bool isDomainLinked)
    {
        var password = await _client.GetManagedAccountPasswordByName(accountName, systemName, isDomainLinked);
        return password.Password;
    }

    public async Task<string> GetPasswordByAccountName(string accountName, string domainName, bool isDomainLinked)
    {
        var password = await _client.GetManagedAccountPasswordByName(accountName, domainName, isDomainLinked);
        return password.Password;
    }
}
```

## Advanced Usage

### Handling Existing Requests

The SDK automatically handles cases where a password request already exists (409 Conflict). It will attempt to find and use the existing request instead of creating a new one.

```csharp
// This will work even if there's already an active request for this account
var password = await _client.GetManagedAccountPasswordById("50");
```

### Checking In Passwords

```csharp
// Check in a password when you're done with it
await _client.CheckInPassword(passwordResult.RequestId, "Task completed");
```

### Retrieving Managed Accounts

```csharp
// Get all managed accounts
var accounts = await _client.GetManagedAccounts();

// Get accounts for a specific system by system ID
var systemAccounts = await _client.GetManagedAccounts("123");

// Get a specific account by system ID and account name
var specificAccount = await _client.GetManagedAccounts("123", "admin");
// This returns a list with a single account if found
```

### Retrieving Managed Systems

```csharp
// Get all managed systems
var systems = await _client.GetManagedSystems();

// Get a specific managed system by ID
var specificSystem = await _client.GetManagedSystems("123");
// This returns a list with a single system if found
```

## Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| BaseUrl | The base URL of your BeyondTrust Password Safe API | Required |
| ApiKey | API key for authentication | Required for API Key auth |
| RunAsUsername | Username for run-as authentication | Required for API Key auth |
| RunAsPassword | Password for run-as authentication | Required for API Key auth |
| UseOAuth | Whether to use OAuth authentication | false |
| OAuthClientId | OAuth client ID | Required for OAuth auth |
| OAuthClientSecret | OAuth client secret | Required for OAuth auth |
| TimeoutSeconds | HTTP request timeout in seconds | 30 |
| DefaultPasswordDuration | Default duration for password requests in minutes | 60 |

## Error Handling

The SDK uses custom exceptions to provide detailed error information:

- `BeyondTrustApiException`: General API errors
- `BeyondTrustAuthenticationException`: Authentication-specific errors

Example:

```csharp
try
{
    var password = await _client.GetManagedAccountPasswordById("50");
}
catch (BeyondTrustApiException ex)
{
    // Handle API errors
    logger.LogError(ex, "Failed to retrieve password");
}
catch (BeyondTrustAuthenticationException ex)
{
    // Handle authentication errors
    logger.LogError(ex, "Authentication failed");
}
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
