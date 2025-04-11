using System;
using Newtonsoft.Json;

namespace BT.PasswordSafe.SDK.Models
{
    /// <summary>
    /// Represents a managed account in BeyondTrust Password Safe
    /// </summary>
    public class ManagedAccount
    {
        /// <summary>
        /// Gets or sets the managed account ID
        /// </summary>
        [JsonProperty("ManagedAccountID")]
        public int ManagedAccountId { get; set; }

        /// <summary>
        /// Gets or sets the account ID (alternative property name used in some API responses)
        /// </summary>
        [JsonProperty("AccountId")]
        public int AccountId { get; set; }

        /// <summary>
        /// Gets or sets the managed system ID
        /// </summary>
        [JsonProperty("ManagedSystemID")]
        public int ManagedSystemId { get; set; }

        /// <summary>
        /// Gets or sets the system ID (alternative property name used in some API responses)
        /// </summary>
        [JsonProperty("SystemId")]
        public int SystemId { get; set; }

        /// <summary>
        /// Gets or sets the domain name for a domain-type account
        /// </summary>
        [JsonProperty("DomainName")]
        public string? DomainName { get; set; }

        /// <summary>
        /// Gets or sets the name of the account
        /// </summary>
        [JsonProperty("AccountName")]
        public string? AccountName { get; set; }

        /// <summary>
        /// Gets or sets the distinguished name of an LDAP managed account
        /// </summary>
        [JsonProperty("DistinguishedName")]
        public string? DistinguishedName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether failed DSS authentication can fall back to password authentication
        /// </summary>
        [JsonProperty("PasswordFallbackFlag")]
        public bool PasswordFallbackFlag { get; set; }

        /// <summary>
        /// Gets or sets the account user principal name of an Active Directory account
        /// </summary>
        [JsonProperty("UserPrincipalName")]
        public string? UserPrincipalName { get; set; }

        /// <summary>
        /// Gets or sets the account SAM account name of an Active Directory account
        /// </summary>
        [JsonProperty("SAMAccountName")]
        public string? SamAccountName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the account should use the managed system login account for SSH sessions
        /// </summary>
        [JsonProperty("LoginAccountFlag")]
        public bool LoginAccountFlag { get; set; }

        /// <summary>
        /// Gets or sets a description of the account
        /// </summary>
        [JsonProperty("Description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the account can be requested through the API
        /// </summary>
        [JsonProperty("ApiEnabled")]
        public bool ApiEnabled { get; set; }

        /// <summary>
        /// Gets or sets the default release duration in minutes
        /// </summary>
        [JsonProperty("ReleaseDuration")]
        public int ReleaseDuration { get; set; }

        /// <summary>
        /// Gets or sets the default maximum release duration in minutes
        /// </summary>
        [JsonProperty("MaxReleaseDuration")]
        public int MaxReleaseDuration { get; set; }

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
        /// Gets or sets a value indicating whether the password is currently changing
        /// </summary>
        [JsonProperty("IsChanging")]
        public bool IsChanging { get; set; }
    }
}
