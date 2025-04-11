using System;
using Newtonsoft.Json;

namespace PRK.BT.PasswordSafe.SDK.Models
{
    /// <summary>
    /// Represents the result of an authentication request to the BeyondTrust Password Safe API
    /// </summary>
    public class AuthenticationResult
    {
        /// <summary>
        /// Gets or sets the authentication token
        /// </summary>
        [JsonProperty("access_token")]
        public string? AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the token type
        /// </summary>
        [JsonProperty("token_type")]
        public string? TokenType { get; set; }

        /// <summary>
        /// Gets or sets the expiration time in seconds
        /// </summary>
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        /// <summary>
        /// Gets or sets the time when the token was issued
        /// </summary>
        [JsonIgnore]
        public DateTimeOffset IssuedAt { get; set; }

        /// <summary>
        /// Gets a value indicating whether the token is expired
        /// </summary>
        [JsonIgnore]
        public bool IsExpired => DateTimeOffset.UtcNow >= IssuedAt.AddSeconds(ExpiresIn);

        /// <summary>
        /// Gets the full authorization header value
        /// </summary>
        [JsonIgnore]
        public string AuthorizationHeaderValue => $"{TokenType} {AccessToken}";
    }
}
