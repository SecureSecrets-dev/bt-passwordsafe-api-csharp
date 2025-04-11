using System;
using Newtonsoft.Json;

namespace PRK.BT.PasswordSafe.SDK.Models
{
    /// <summary>
    /// Represents the result of a password request to BeyondTrust Password Safe
    /// </summary>
    public class PasswordRequestResult
    {
        /// <summary>
        /// Gets or sets the request ID
        /// </summary>
        [JsonProperty("RequestID")]
        public string? RequestId { get; set; }

        /// <summary>
        /// Gets or sets the request status
        /// </summary>
        [JsonProperty("Status")]
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the creation date
        /// </summary>
        [JsonProperty("CreatedDate")]
        public DateTimeOffset CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the expiration date
        /// </summary>
        [JsonProperty("ExpirationDate")]
        public DateTimeOffset ExpirationDate { get; set; }

        /// <summary>
        /// Gets or sets the system ID
        /// </summary>
        [JsonProperty("SystemID")]
        public int SystemId { get; set; }

        /// <summary>
        /// Gets or sets the account ID
        /// </summary>
        [JsonProperty("AccountID")]
        public int AccountId { get; set; }

        /// <summary>
        /// Gets or sets the requester ID
        /// </summary>
        [JsonProperty("RequesterID")]
        public int RequesterId { get; set; }

        /// <summary>
        /// Gets or sets the approver ID
        /// </summary>
        [JsonProperty("ApproverID")]
        public int? ApproverId { get; set; }

        /// <summary>
        /// Gets or sets the requester name
        /// </summary>
        [JsonProperty("RequesterName")]
        public string? RequesterName { get; set; }

        /// <summary>
        /// Gets or sets the approver name
        /// </summary>
        [JsonProperty("ApproverName")]
        public string? ApproverName { get; set; }

        /// <summary>
        /// Gets or sets the access type
        /// </summary>
        [JsonProperty("AccessType")]
        public string? AccessType { get; set; }
    }
}
