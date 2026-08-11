namespace JMAPI.Models
{
    public enum RefreshTokenRevocationReason
    {
        /// <summary>Replaced by a newer token during a normal refresh.</summary>
        Rotated = 0,
        /// <summary>The user signed out, or changed their password.</summary>
        SignedOut = 1,
        /// <summary>Killed defensively - token reuse detected, or account deactivated.</summary>
        SecuritySweep = 2
    }

    /// <summary>
    /// One row per signed-in session (device/browser), replacing the single
    /// RefreshToken column that used to live on AppUser. That column allowed
    /// only one active session per user, so signing in on a phone silently
    /// signed the same user out on their desktop.
    /// </summary>
    public class RefreshToken
    {
        public int Id { get; set; }

        public string AppUserId { get; set; } = string.Empty;
        public AppUser? AppUser { get; set; }

        /// <summary>
        /// Base64 SHA-256 hash of the token. The raw value is only ever held by
        /// the client, so a leaked database dump can't be replayed as a session.
        /// </summary>
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// True when the user ticked "Keep me logged in". Persistent sessions get
        /// the long refresh window and a persistent jwt cookie; non-persistent
        /// ones get a short window and a browser-session cookie. Carried across
        /// rotations so a session keeps the policy it was created with.
        /// </summary>
        public bool IsPersistent { get; set; }

        /// <summary>
        /// Set when the token is rotated (on refresh) or revoked (on logout).
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Why the token was revoked. Only <see cref="RefreshTokenRevocationReason.Rotated"/>
        /// gets the grace window that keeps racing browser tabs alive - a token
        /// killed by a sign-out has to stop working immediately.
        /// </summary>
        public RefreshTokenRevocationReason? RevokedReason { get; set; }

        /// <summary>
        /// Truncated user agent, so a future "your active sessions" screen can
        /// tell devices apart. Purely informational.
        /// </summary>
        public string? DeviceLabel { get; set; }

        public bool IsExpired(DateTime utcNow) => ExpiresAt <= utcNow;

        public bool IsActive(DateTime utcNow) => RevokedAt == null && !IsExpired(utcNow);
    }
}
