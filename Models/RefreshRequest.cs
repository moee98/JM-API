namespace JMAPI.Models
{
    public class RefreshRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LogoutRequest
    {
        /// <summary>
        /// The refresh token of the session being signed out. Optional: when
        /// omitted every session for the user is revoked, which is also the
        /// behaviour older clients get.
        /// </summary>
        public string? RefreshToken { get; set; }
    }
}
