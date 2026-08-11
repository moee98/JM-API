namespace JMAPI.Models
{
    public class AuthModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Name { get; set; }
        /// <summary>
        /// "Keep me logged in". When true the session survives browser restarts
        /// and lasts for Auth:PersistentRefreshTokenDays; when false it lasts
        /// for Auth:SessionRefreshTokenDays and dies with the browser session.
        /// </summary>
        public bool RememberMe { get; set; }
    }
}
