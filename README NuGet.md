# BT.PasswordSafe.SDK

A modern .NET SDK for interacting with BeyondTrust Password Safe API. This SDK provides a simple and intuitive interface for retrieving and managing passwords from BeyondTrust Password Safe.

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
dotnet add package BT.PasswordSafe.SDK
```

## Quick Start

### Add Required Namespaces

```csharp
using BT.PasswordSafe.SDK;
using BT.PasswordSafe.SDK.Extensions;
using BT.PasswordSafe.SDK.Interfaces;
using BT.PasswordSafe.SDK.Models;
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

## Usage Examples

### Authentication

```csharp
// Authenticate with the Password Safe API
var authResult = await client.Authenticate();
Console.WriteLine($"Token Type: {authResult.TokenType}");
Console.WriteLine($"Expires In: {authResult.ExpiresIn} seconds");
```

### Retrieving Managed Accounts

```csharp
// Get all managed accounts
var accounts = await client.GetManagedAccounts();

// Get accounts for a specific system by system ID
var systemAccounts = await client.GetManagedAccounts("123");

// Get a specific account by system ID and account name
var specificAccount = await client.GetManagedAccounts("123", "admin");
```

### Retrieving Managed Systems

```csharp
// Get all managed systems
var systems = await client.GetManagedSystems();

// Get a specific managed system by ID
var specificSystem = await client.GetManagedSystems("123");
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

### Checking In Passwords

```csharp
// Check in a password when you're done with it
await client.CheckInPassword(password.RequestId, "Task completed");
```

## Error Handling

```csharp
try
{
    var password = await client.GetManagedAccountPasswordById("50");
}
catch (BeyondTrustApiException ex)
{
    // Handle API errors
    Console.WriteLine($"API Error: {ex.Message}");
}
catch (BeyondTrustAuthenticationException ex)
{
    // Handle authentication errors
    Console.WriteLine($"Authentication Error: {ex.Message}");
}
```

## License

This project is licensed under the MIT License.
