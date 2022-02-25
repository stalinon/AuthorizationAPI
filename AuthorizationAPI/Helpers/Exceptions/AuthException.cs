using System.Globalization;

namespace AuthorizationAPI.Helpers.Exceptions
{
    public class AuthException : Exception
    {
        public AuthException() : base() { }

        public AuthException(string message) : base(message) { }

        public AuthException(string message, params object[] args)
            : base(string.Format(CultureInfo.CurrentCulture, message, args))
        {
        }
    }
}
