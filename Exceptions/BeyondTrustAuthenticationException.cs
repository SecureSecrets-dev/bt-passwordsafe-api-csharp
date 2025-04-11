using System;

namespace PRK.BT.PasswordSafe.SDK.Exceptions
{
    /// <summary>
    /// Exception thrown when authentication with the BeyondTrust Password Safe API fails
    /// </summary>
    public class BeyondTrustAuthenticationException : BeyondTrustApiException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BeyondTrustAuthenticationException"/> class
        /// </summary>
        public BeyondTrustAuthenticationException() : base("Authentication with the BeyondTrust Password Safe API failed")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BeyondTrustAuthenticationException"/> class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public BeyondTrustAuthenticationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BeyondTrustAuthenticationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public BeyondTrustAuthenticationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
