# BT.PasswordSafe.API

A .NET package for interacting with BeyondTrust Password Safe API. This package provides a simple and intuitive interface for retrieving passwords, managed accounts and managed systems from BeyondTrust Password Safe.

## Features

- 🔐 **Authentication**: Support for both API Key and OAuth authentication methods
- 🔄 **Token Management**: Handles token refresh and expiration automatically
- 🔍 **Managed Accounts**: Find and manage accounts by ID, name, or system
- 🔎 **Managed Systems**: Retrieve managed systems by ID or get a complete list
- 🔑 **Password Retrieval**: Get passwords with automatic request handling and conflict resolution
- 🧩 **Error Handling**: Gracefully handles API errors including 409 Conflict scenarios
- 📝 **Detailed Logging**: Comprehensive logging for troubleshooting and auditing
- 🧰 **Dependency Injection**: Seamlessly integrates with Microsoft's dependency injection
- ⚡ **Full Async Support**: Complete async/await pattern implementation for all operations
- 🛡️ **Type Safety**: Strongly-typed models for all API interactions

## Installation

```bash
dotnet add package BT.PasswordSafe.API
```

## Quick Start

### Add Required Namespaces

```csharp
using BT.PasswordSafe.API;
using BT.PasswordSafe.API.Extensions;
using BT.PasswordSafe.API.Interfaces;
using BT.PasswordSafe.API.Models;
using Microsoft.Extensions.DependencyInjection;
```

### Register Services

```csharp
// Create a service collection
var services = new ServiceCollection();

// Add the PasswordSafe client
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

// Build the service provider
var serviceProvider = services.BuildServiceProvider();

// Get the client from the service provider
var client = serviceProvider.GetRequiredService<IPasswordSafeClient>();
```

### Configuration Options

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
| AutoRefreshToken | Whether to automatically refresh the OAuth token | true |

### Using with ASP.NET Core

In `Program.cs` or `Startup.cs`:

```csharp
// Add the PasswordSafe client to the service collection
builder.Services.AddPasswordSafeClient(options => 
    builder.Configuration.GetSection("PasswordSafe").Bind(options));
```

In your `appsettings.json`:

```json
{
  "PasswordSafe": {
    "BaseUrl": "https://your-instance.beyondtrustcloud.com/BeyondTrust/api/public/v3/",
    "ApiKey": "your-api-key",
    "RunAsUsername": "your-username",
    "RunAsPassword": "your-password",
    "UseOAuth": false,
    "TimeoutSeconds": 30,
    "DefaultPasswordDuration": 60,
    "AutoRefreshToken": true
  }
}
```

In your controller or service:

```csharp
public class PasswordService
{
    private readonly IPasswordSafeClient _client;
    
    public PasswordService(IPasswordSafeClient client)
    {
        _client = client; // Injected by the DI container
    }
    
    public async Task<string> GetPasswordByAccountId(string accountId)
    {
        var password = await _client.GetManagedAccountPasswordById(accountId);
        return password.Password;
    }
}
```

### Authentication

```csharp
// Authenticate with the Password Safe API
var authResult = await client.Authenticate();
Console.WriteLine($"Token Type: {authResult.TokenType}");
Console.WriteLine($"Expires In: {authResult.ExpiresIn} seconds");
```

The SDK supports two authentication methods:

1. **PS-Auth Authentication**: Uses the API Key, RunAs Username, and RunAs Password
2. **OAuth Authentication**: Uses Client ID and Client Secret for OAuth 2.0 authentication

The authentication method is determined by the `UseOAuth` option. When set to `true`, OAuth authentication is used; otherwise, API key authentication is used.

## Advanced Usage

### Handling Existing Requests

The SDK automatically handles cases where a password request already exists (409 Conflict). It will attempt to find and use the existing request instead of creating a new one.

```csharp
// This will work even if there's already an active request for this account
var password = await client.GetManagedAccountPasswordById("50");
```

### Checking In Passwords

```csharp
// Check in a password when you're done with it
await client.CheckInPassword(passwordResult.RequestId, "Task completed");
```

### Retrieving Managed Accounts

```csharp
// Get all managed accounts
var accounts = await client.GetManagedAccounts();

// Get accounts for a specific system by system ID
var systemAccounts = await client.GetManagedAccounts("123");

// Get a specific account by system ID and account name
var specificAccount = await client.GetManagedAccounts("123", "admin");
// This returns a list with a single account if found
```

### Retrieving Managed Systems

```csharp
// Get all managed systems
var systems = await client.GetManagedSystems();

// Get a specific managed system by ID
var specificSystem = await client.GetManagedSystems("123");
// This returns a list with a single system if found
```

### Retrieving Passwords

```csharp
// Get password by account ID
var password = await client.GetManagedAccountPasswordById("50");
Console.WriteLine($"Password: {password.Password}");
Console.WriteLine($"Request ID: {password.RequestId}");
Console.WriteLine($"Expires: {password.ExpirationDate}");

// Get password by account name and system name
var password = await client.GetManagedAccountPasswordByName("admin", "DC01");
```

## Error Handling

The SDK uses custom exceptions to provide detailed error information:

- `BeyondTrustApiException`: General API errors
- `BeyondTrustAuthenticationException`: Authentication-specific errors

Example:

```csharp
try
{
    var password = await client.GetManagedAccountPasswordById("50");
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

This project is licensed under the MIT License.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
