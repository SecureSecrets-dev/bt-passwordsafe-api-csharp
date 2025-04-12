using System;
using Newtonsoft.Json;

namespace BT.PasswordSafe.API.Models
{
    /// <summary>
    /// Represents a password request to BeyondTrust Password Safe
    /// </summary>
    public class PasswordRequest
    {
        /// <summary>
        /// Gets or sets the type of access requested (View, RDP, SSH, App)
        /// </summary>
        [JsonProperty("AccessType")]
        public string? AccessType { get; set; } = "View";

        /// <summary>
        /// Gets or sets the ID of the managed system to request
        /// </summary>
        [JsonProperty("SystemID")]
        public int SystemId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the managed account to request
        /// </summary>
        [JsonProperty("AccountID")]
        public int AccountId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the application for an application-based request
        /// </summary>
        [JsonProperty("ApplicationID")]
        public int? ApplicationId { get; set; }

        /// <summary>
        /// Gets or sets the request duration (in minutes)
        /// </summary>
        [JsonProperty("DurationMinutes")]
        public int DurationMinutes { get; set; }

        /// <summary>
        /// Gets or sets the reason for the request
        /// </summary>
        [JsonProperty("Reason")]
        public string? Reason { get; set; }

        /// <summary>
        /// Gets or sets the schedule ID of an access policy to use for the request
        /// </summary>
        [JsonProperty("AccessPolicyScheduleID")]
        public int? AccessPolicyScheduleId { get; set; }

        /// <summary>
        /// Gets or sets the conflict resolution option
        /// </summary>
        [JsonProperty("ConflictOption")]
        public string? ConflictOption { get; set; }

        /// <summary>
        /// Gets or sets the ID of the ticket system
        /// </summary>
        [JsonProperty("TicketSystemID")]
        public int? TicketSystemId { get; set; }

        /// <summary>
        /// Gets or sets the number of associated ticket
        /// </summary>
        [JsonProperty("TicketNumber")]
        public string? TicketNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to rotate the credentials on check-in/expiry
        /// </summary>
        [JsonProperty("RotateOnCheckin")]
        public bool RotateOnCheckin { get; set; } = true;
    }
}
