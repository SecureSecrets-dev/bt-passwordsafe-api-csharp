using System;

namespace BT.PasswordSafe.API.Models
{
    /// <summary>
    /// Configuration options for the Password Safe client
    /// </summary>
    public class PasswordSafeOptions
    {
        /// <summary>
        /// Gets or sets the base URL of the Password Safe API
        /// </summary>
        /// <remarks>
        /// For on-premises: https://your-server/BeyondTrust/api/public/v3
        /// For cloud: https://your-cloud-instance-url/BeyondTrust/api/public/v3
        /// </remarks>
        public string? BaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the API key configured in BeyondInsight for your application
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the username of a BeyondInsight user that has been granted permission to use the API key
        /// </summary>
        public string? RunAsUsername { get; set; }

        /// <summary>
        /// Gets or sets the RunAs user password
        /// </summary>
        public string? RunAsPassword { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use OAuth authentication instead of PS-Auth
        /// </summary>
        public bool UseOAuth { get; set; }

        /// <summary>
        /// Gets or sets the OAuth client ID (only used when UseOAuth is true)
        /// </summary>
        public string? OAuthClientId { get; set; }

        /// <summary>
        /// Gets or sets the OAuth client secret (only used when UseOAuth is true)
        /// </summary>
        public string? OAuthClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the timeout for HTTP requests in seconds
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets a value indicating whether to automatically refresh the authentication token before it expires
        /// </summary>
        public bool AutoRefreshToken { get; set; } = true;

        /// <summary>
        /// Gets or sets the default duration in minutes for password requests
        /// </summary>
        public int DefaultPasswordDuration { get; set; } = 60;

        /// <summary>
        /// Gets or sets the buffer time in minutes before token expiration when a refresh should occur
        /// </summary>
        /// <remarks>
        /// Default is 5 minutes. Setting a lower value may result in more frequent token refreshes,
        /// while a higher value ensures tokens are refreshed well before they expire.
        /// </remarks>
        public int TokenBufferMinutes { get; set; } = 5;

        /// <summary>
        /// Validates the configuration options
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when required options are missing or invalid</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(BaseUrl))
            {
                throw new ArgumentException("BaseUrl is required", nameof(BaseUrl));
            }

            if (UseOAuth)
            {
                if (string.IsNullOrWhiteSpace(OAuthClientId))
                {
                    throw new ArgumentException("OAuthClientId is required when UseOAuth is true", nameof(OAuthClientId));
                }

                if (string.IsNullOrWhiteSpace(OAuthClientSecret))
                {
                    throw new ArgumentException("OAuthClientSecret is required when UseOAuth is true", nameof(OAuthClientSecret));
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(RunAsUsername))
                {
                    throw new ArgumentException("RunAsUsername is required when UseOAuth is false", nameof(RunAsUsername));
                }

                if (string.IsNullOrWhiteSpace(ApiKey))
                {
                    throw new ArgumentException("ApiKey is required", nameof(ApiKey));
                }
            }
        }
    }
}
