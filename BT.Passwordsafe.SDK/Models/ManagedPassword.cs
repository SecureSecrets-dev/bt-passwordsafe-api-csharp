using System;
using Newtonsoft.Json;

namespace BT.PasswordSafe.SDK.Models
{
    /// <summary>
    /// Represents a managed password retrieved from BeyondTrust Password Safe
    /// </summary>
    public class ManagedPassword
    {
        /// <summary>
        /// Gets or sets the password value
        /// </summary>
        [JsonProperty("Password")]
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets the DSS key value (if applicable)
        /// </summary>
        [JsonProperty("DSSKey")]
        public string? DssKey { get; set; }

        /// <summary>
        /// Gets or sets the request ID associated with this password
        /// </summary>
        [JsonProperty("RequestID")]
        public string? RequestId { get; set; }

        /// <summary>
        /// Gets or sets the account ID
        /// </summary>
        [JsonProperty("AccountID")]
        public int AccountId { get; set; }

        /// <summary>
        /// Gets or sets the system ID
        /// </summary>
        [JsonProperty("SystemID")]
        public int SystemId { get; set; }

        /// <summary>
        /// Gets or sets the expiration date of the password request
        /// </summary>
        [JsonProperty("ExpirationDate")]
        public DateTimeOffset ExpirationDate { get; set; }
    }
}
