using System;
using Newtonsoft.Json;

namespace PRK.BT.PasswordSafe.SDK.Models
{
    /// <summary>
    /// Represents a managed system in BeyondTrust Password Safe
    /// </summary>
    public class ManagedSystem
    {
        /// <summary>
        /// Gets or sets the managed system ID
        /// </summary>
        [JsonProperty("ManagedSystemID")]
        public int ManagedSystemId { get; set; }

        /// <summary>
        /// Gets or sets the asset ID
        /// </summary>
        [JsonProperty("AssetID")]
        public int? AssetId { get; set; }

        /// <summary>
        /// Gets or sets the database ID
        /// </summary>
        [JsonProperty("DatabaseID")]
        public int? DatabaseId { get; set; }

        /// <summary>
        /// Gets or sets the system name
        /// </summary>
        [JsonProperty("SystemName")]
        public string? SystemName { get; set; }

        /// <summary>
        /// Gets or sets the display name
        /// </summary>
        [JsonProperty("DisplayName")]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the domain name
        /// </summary>
        [JsonProperty("DomainName")]
        public string? DomainName { get; set; }

        /// <summary>
        /// Gets or sets the system description
        /// </summary>
        [JsonProperty("Description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the port
        /// </summary>
        [JsonProperty("Port")]
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the system is enabled
        /// </summary>
        [JsonProperty("Enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the last password change date
        /// </summary>
        [JsonProperty("LastChangeDate")]
        public DateTime? LastChangeDate { get; set; }

        /// <summary>
        /// Gets or sets the next scheduled password change date
        /// </summary>
        [JsonProperty("NextChangeDate")]
        public DateTime? NextChangeDate { get; set; }

        /// <summary>
        /// Gets or sets the platform ID
        /// </summary>
        [JsonProperty("PlatformID")]
        public int PlatformId { get; set; }

        /// <summary>
        /// Gets or sets the platform name
        /// </summary>
        [JsonProperty("PlatformName")]
        public string? PlatformName { get; set; }

        /// <summary>
        /// Gets or sets the NetBIOS name
        /// </summary>
        [JsonProperty("NetBiosName")]
        public string? NetBiosName { get; set; }

        /// <summary>
        /// Gets or sets the IP address
        /// </summary>
        [JsonProperty("IPAddress")]
        public string? IpAddress { get; set; }

        /// <summary>
        /// Gets or sets the DNS name
        /// </summary>
        [JsonProperty("DNSName")]
        public string? DnsName { get; set; }

        /// <summary>
        /// Gets or sets the instance name
        /// </summary>
        [JsonProperty("InstanceName")]
        public string? InstanceName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the system is a directory
        /// </summary>
        [JsonProperty("IsDirectory")]
        public bool IsDirectory { get; set; }
    }
}
