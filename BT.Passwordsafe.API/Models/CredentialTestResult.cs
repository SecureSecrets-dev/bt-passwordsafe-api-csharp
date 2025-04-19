using System;
using Newtonsoft.Json;

namespace BT.PasswordSafe.API.Models
{
    /// <summary>
    /// Represents the result of a credential test operation
    /// </summary>
    public class CredentialTestResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the credential test succeeded
        /// </summary>
        [JsonProperty("Success")]
        public bool Success { get; set; }
    }
}
