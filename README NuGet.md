# BT.PasswordSafe.API

A .NET package for interacting with BeyondTrust Password Safe API. This package provides a simple and intuitive interface for retrieving passwords, secrets, managed accounts and managed systems from BeyondTrust Password Safe.

## Features

- 🔐 **Authentication**: Support for both API Key and OAuth authentication methods
- 🔄 **Token Management**: Handles token refresh and expiration automatically
- 🔍 **Managed Accounts**: Find and manage accounts by ID, name, or system
- 🔎 **Managed Systems**: Retrieve managed systems by ID or get a complete list
- 🔑 **Password Retrieval**: Get passwords with automatic request handling and conflict resolution
- 🧩 **Error Handling**: Gracefully handles API errors including 409 Conflict scenarios
- 📝 **Detailed Logging**: Comprehensive logging for troubleshooting and auditing
- 🛡️ **Type Safety**: Strongly-typed models for all API interactions
- 🔒 **Secret Retrieval**: Get secrets by ID or name (title)

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
// Create a service collection
var services = new ServiceCollection();

// Add the PasswordSafe client
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
| RunAsPassword | Password for run-as authentication | Required for API Key auth |
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
Refer to github repository for more details: https://github.com/keertipatip/bt-passwordsafe-api-csharp

## License

This project is licensed under the MIT License.
