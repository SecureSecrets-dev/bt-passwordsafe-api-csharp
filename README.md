# BT.PasswordSafe.API

A .NET package for interacting with BeyondTrust Password Safe API. This package provides a simple and intuitive interface for retrieving passwords, managed accounts and managed systems from BeyondTrust Password Safe.

[![NuGet](https://img.shields.io/nuget/v/BT.PasswordSafe.API.svg)](https://www.nuget.org/packages/BT.PasswordSafe.API/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Features

- 🔐 **Authentication**: Support for both API Key and OAuth authentication methods
- 🔄 **Token Management**: Handles token refresh and expiration automatically
- 🔍 **Managed Accounts**: Find and manage accounts by ID, name, or system
- 🔎 **Managed Systems**: Retrieve managed systems by ID or get a complete list
- 🔑 **Password Retrieval**: Get passwords with automatic request handling and conflict resolution
- 🧩 **Error Handling**: Gracefully handles API errors including 409 Conflict scenarios
- 📝 **Detailed Logging**: Comprehensive logging for troubleshooting and auditing
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
```

### Register Services

```csharp
// Add to your service collection
services.AddPasswordSafeClient(options =>
{
    options.BaseUrl = "https://your-instance.ps.beyondtrustcloud.com/BeyondTrust/api/public/v3/";
    //options.BaseUrl = "https://your-instance/BeyondTrust/api/public/v3/";
    
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

### Alternate Registration Method using appsettings.json

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
    "BaseUrl": "https://your-instance.ps.beyondtrustcloud.com/BeyondTrust/api/public/v3/",
    //"BaseUrl": "https://your-instance/BeyondTrust/api/public/v3/",
    "ApiKey": "your-api-key",
    "RunAsUsername": "your-username",
    "RunAsPassword": "your-password",
    "UseOAuth": false,
    "OAuthClientId": "your-client-id",
    "OAuthClientSecret": "your-client-secret",
    "TimeoutSeconds": 30,
    "DefaultPasswordDuration": 60,
    "AutoRefreshToken": true
  }
}
```

### Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| BaseUrl | The base URL of your BeyondTrust Password Safe API | Required |
| ApiKey | API key for authentication | Required for API Key auth |
| RunAsUsername | Username for run-as authentication | Required for API Key auth |
| RunAsPassword | Password for run-as authentication | Optional |
| UseOAuth | Whether to use OAuth authentication | false |
| OAuthClientId | OAuth client ID | Required for OAuth auth |
| OAuthClientSecret | OAuth client secret | Required for OAuth auth |
| TimeoutSeconds | HTTP request timeout in seconds | 30 |
| DefaultPasswordDuration | Default duration for password requests in minutes | 60 |
| AutoRefreshToken | Whether to automatically refresh the OAuth token | true |

### Initialize the Client

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

The SDK handles authentication automatically when you make API calls. However, you can also explicitly authenticate if needed:

```csharp
// Authenticate with the Password Safe API
var authResult = await _client.Authenticate();

// The authentication result contains the token information
Console.WriteLine($"Token Type: {authResult.TokenType}");
Console.WriteLine($"Expires In: {authResult.ExpiresIn} seconds");
```

The SDK supports two authentication methods:

1. **PS-Auth Authentication**: Uses the API Key, RunAs Username, and RunAs Password
2. **OAuth Authentication**: Uses Client ID and Client Secret for OAuth 2.0 authentication

The authentication method is determined by the `UseOAuth` option. When set to `true`, OAuth authentication is used; otherwise, API key authentication is used.

### Retrieving Passwords

```csharp
// Get password by account ID with a reason
var password = await _client.GetManagedAccountPasswordById("50", reason: "Support ticket #1234 - scheduled maintenance");
Console.WriteLine($"Password: {password.Password}");
Console.WriteLine($"Request ID: {password.RequestId}");
Console.WriteLine($"Expires: {password.ExpirationDate}");

// Get password by account name and system name with a reason
var passwordByName = await _client.GetManagedAccountPasswordByName("admin", "DC01", reason: "Support ticket #1234 - scheduled maintenance");
Console.WriteLine($"Password: {passwordByName.Password}");
Console.WriteLine($"Request ID: {passwordByName.RequestId}");
Console.WriteLine($"Expires: {passwordByName.ExpirationDate}");

// Get password by account name and domain name where isDomainLinked is true
var passwordByDomain = await _client.GetManagedAccountPasswordByName(
    "admin",
    domainName: "domain.com",
    isDomainLinked: true,
    reason: "Support ticket #1234 - domain account access"
);
Console.WriteLine($"Password: {passwordByDomain.Password}");
Console.WriteLine($"Request ID: {passwordByDomain.RequestId}");
Console.WriteLine($"Expires: {passwordByDomain.ExpirationDate}");

// Get password by request ID (useful when you've already created a request)
var requestId = "89"; // Request ID from a previous CreatePasswordRequest call or retrieval
var passwordByRequestId = await _client.GetManagedAccountPasswordByRequestId(requestId, reason: "Support ticket #1234 - scheduled maintenance");
Console.WriteLine($"Password: {passwordByRequestId.Password}");
Console.WriteLine($"Expires: {passwordByRequestId.ExpirationDate}");
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

## Test Application

The solution includes a test application (`BT.PasswordSafe.API.TestApp`) that demonstrates all the key features of the SDK. You can use this app to verify your configuration and test the SDK functionality against your BeyondTrust Password Safe instance.

### Running the Test App

1. **Configure the application**:
   - Edit the `appsettings.json` file in the `BT.PasswordSafe.API.TestApp` directory
   - Update the `PasswordSafe` section with your instance details:
     ```json
     "PasswordSafe": {
       "BaseUrl": "https://your-instance.ps.beyondtrustcloud.com/BeyondTrust/api/public/v3/",
       //"BaseUrl": "https://your-instance/BeyondTrust/api/public/v3/",
       
       // For API Key authentication
       "ApiKey": "your-api-key",
       "RunAsUsername": "your-username",
       "RunAsPassword": "your-password",
       "UseOAuth": false,
       
       // For OAuth authentication
       "UseOAuth": true,
       "OAuthClientId": "your-client-id",
       "OAuthClientSecret": "your-client-secret"
     }
     ```
   - Update the `TestSettings` section with valid test data for your environment:
     ```json
     "TestSettings": {
       "SystemId": "123",
       "AccountId": "50",
       "AccountName": "admin",
       "SystemName": "YourSystem"
     }
     ```

2. **Build and run the application**:
   ```bash
   cd BT.PasswordSafe.API.TestApp
   dotnet run
   ```

3. **Using the compiled executable**:
   - The application will look for `appsettings.json` in the same directory as the executable
   - If you're running the compiled executable directly, make sure to copy the `appsettings.json` file to the same directory
   - The project is configured to automatically copy the `appsettings.json` file to the output directory during build

### Features Demonstrated

The test application demonstrates all the key features of the SDK:

- Authentication (both API Key and OAuth)
- Retrieving managed systems (all systems and by ID)
- Retrieving managed accounts (all accounts, by system, by ID, by name)
- Password retrieval and management
- Error handling and logging

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
