using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BT.PasswordSafe.API.Models
{
    /// <summary>
    /// Represents a secret stored in BeyondTrust Password Safe.
    /// </summary>
    public class SecretSafe
    {
        /// <summary>
        /// Unique identifier of the secret.
        /// </summary>
        [JsonProperty("Id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Title or name of the secret.
        /// </summary>
        [JsonProperty("Title")]
        public string? Title { get; set; }

        /// <summary>
        /// Description of the secret.
        /// </summary>
        [JsonProperty("Description")]
        public string? Description { get; set; }

        /// <summary>
        /// Username associated with the secret.
        /// </summary>
        [JsonProperty("Username")]
        public string? Username { get; set; }

        /// <summary>
        /// Password value of the secret.
        /// </summary>
        [JsonProperty("Password")]
        public string? Password { get; set; }

        /// <summary>
        /// Name of the file attached to the secret, if any.
        /// </summary>
        [JsonProperty("FileName")]
        public string? FileName { get; set; }

        /// <summary>
        /// Hash of the attached file, if any.
        /// </summary>
        [JsonProperty("FileHash")]
        public string? FileHash { get; set; }

        /// <summary>
        /// Text value of the secret, if applicable.
        /// </summary>
        [JsonProperty("Text")]
        public string? Text { get; set; }

        /// <summary>
        /// Type of the secret (e.g., password, document).
        /// </summary>
        [JsonProperty("SecretType")]
        public string? SecretType { get; set; }

        /// <summary>
        /// Identifier of the owner of the secret.
        /// </summary>
        [JsonProperty("OwnerId")]
        public int? OwnerId { get; set; }

        /// <summary>
        /// Identifier of the folder containing the secret.
        /// </summary>
        [JsonProperty("FolderId")]
        public Guid FolderId { get; set; }

        /// <summary>
        /// Date and time the secret was created.
        /// </summary>
        [JsonProperty("CreatedOn")]
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Identifier of the user who created the secret.
        /// </summary>
        [JsonProperty("CreatedBy")]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Date and time the secret was last modified.
        /// </summary>
        [JsonProperty("ModifiedOn")]
        public DateTime ModifiedOn { get; set; }

        /// <summary>
        /// Identifier of the user who last modified the secret.
        /// </summary>
        [JsonProperty("ModifiedBy")]
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Owner object with details about the owner.
        /// </summary>
        [JsonProperty("Owner")]
        public string? Owner { get; set; }

        /// <summary>
        /// Folder object with details about the folder.
        /// </summary>
        [JsonProperty("Folder")]
        public string? Folder { get; set; }

        /// <summary>
        /// Path of the folder containing the secret.
        /// </summary>
        [JsonProperty("FolderPath")]
        public string? FolderPath { get; set; }

        /// <summary>
        /// List of owners for the secret.
        /// </summary>
        [JsonProperty("Owners")]
        public List<SecretSafeOwner>? Owners { get; set; }

        /// <summary>
        /// Type of the owner (user or group).
        /// </summary>
        [JsonProperty("OwnerType")]
        public string? OwnerType { get; set; }

        /// <summary>
        /// Notes associated with the secret.
        /// </summary>
        [JsonProperty("Notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// List of URLs associated with the secret.
        /// </summary>
        [JsonProperty("Urls")]
        public List<SecretSafeUrl>? Urls { get; set; }
    }

    /// <summary>
    /// Represents an owner of a secret in Password Safe.
    /// </summary>
    public class SecretSafeOwner
    {
        /// <summary>
        /// Unique identifier of the owner.
        /// </summary>
        [JsonProperty("OwnerId")]
        public int? OwnerId { get; set; }

        /// <summary>
        /// Name of the owner.
        /// </summary>
        [JsonProperty("Owner")]
        public string? Owner { get; set; }

        /// <summary>
        /// Email address of the owner.
        /// </summary>
        [JsonProperty("Email")]
        public string? Email { get; set; }

        /// <summary>
        /// Group identifier, if the owner is a group.
        /// </summary>
        [JsonProperty("GroupId")]
        public int? GroupId { get; set; }

        /// <summary>
        /// User identifier, if the owner is a user.
        /// </summary>
        [JsonProperty("UserId")]
        public int? UserId { get; set; }

        /// <summary>
        /// Display name of the owner.
        /// </summary>
        [JsonProperty("Name")]
        public string? Name { get; set; }
    }

    /// <summary>
    /// Represents a URL associated with a secret in Password Safe.
    /// </summary>
    public class SecretSafeUrl
    {
        /// <summary>
        /// Unique identifier of the URL entry.
        /// </summary>
        [JsonProperty("Id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Identifier of the credential associated with the URL.
        /// </summary>
        [JsonProperty("CredentialId")]
        public Guid CredentialId { get; set; }

        /// <summary>
        /// The URL value.
        /// </summary>
        [JsonProperty("Url")]
        public string? Url { get; set; }
    }
}
