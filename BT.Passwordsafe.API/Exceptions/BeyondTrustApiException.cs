using System;

namespace BT.PasswordSafe.API.Exceptions
{
    /// <summary>
    /// Exception thrown when an error occurs while communicating with the BeyondTrust Password Safe API
    /// </summary>
    public class BeyondTrustApiException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BeyondTrustApiException"/> class
        /// </summary>
        public BeyondTrustApiException() : base("An error occurred while communicating with the BeyondTrust Password Safe API")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BeyondTrustApiException"/> class with a specified error message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public BeyondTrustApiException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BeyondTrustApiException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public BeyondTrustApiException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
