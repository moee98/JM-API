using JMAPI.Database;
using JMAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace JMAPI.Services
{
    public sealed record RefreshOutcome(
        bool Succeeded,
        string? RawToken,
        AppUser? User,
        bool IsPersistent,
        DateTime ExpiresAt)
    {
        public static RefreshOutcome Failed() =>
            new(false, null, null, false, DateTime.MinValue);
    }

    /// <summary>
    /// Issues, rotates and revokes the per-session refresh tokens in the
    /// RefreshTokens table. Sessions are a sliding window: every successful
    /// refresh mints a new token and pushes the expiry out again, so an active
    /// user stays signed in until they explicitly sign out.
    /// </summary>
    public class RefreshTokenService
    {
        /// <summary>
        /// How long a just-rotated token keeps working. Two browser tabs can
        /// both hit /auth/refresh within milliseconds of each other; without
        /// this window the loser of that race would be signed out even though
        /// nothing was actually wrong with its token.
        /// </summary>
        public static readonly TimeSpan RotationGrace = TimeSpan.FromSeconds(60);

        // Revoked rows are kept this long so token reuse is still detectable
        // after the fact; expired rows are dropped as soon as they lapse.
        private static readonly TimeSpan RevokedRetention = TimeSpan.FromDays(2);

        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly ILogger<RefreshTokenService> _logger;

        public RefreshTokenService(
            AppDbContext db,
            IConfiguration config,
            ILogger<RefreshTokenService> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        /// <summary>Lifetime of a "keep me logged in" session. Default one year.</summary>
        public TimeSpan PersistentLifetime =>
            TimeSpan.FromDays(_config.GetValue<int?>("Auth:PersistentRefreshTokenDays") ?? 365);

        /// <summary>Lifetime of a session the user didn't ask to be remembered.</summary>
        public TimeSpan SessionLifetime =>
            TimeSpan.FromDays(_config.GetValue<int?>("Auth:SessionRefreshTokenDays") ?? 1);

        public TimeSpan LifetimeFor(bool isPersistent) =>
            isPersistent ? PersistentLifetime : SessionLifetime;

        public static string Hash(string rawToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToBase64String(bytes);
        }

        private static string GenerateRawToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        /// <summary>
        /// Creates a brand new session for the user and returns the raw token,
        /// which is the only place that value ever exists outside the client.
        /// </summary>
        public async Task<RefreshOutcome> IssueAsync(
            AppUser user,
            bool isPersistent,
            string? deviceLabel,
            CancellationToken cancellationToken = default)
        {
            await PruneAsync(user.Id, cancellationToken);

            var rawToken = GenerateRawToken();
            var now = DateTime.UtcNow;
            var expiresAt = now.Add(LifetimeFor(isPersistent));

            _db.RefreshTokens.Add(new RefreshToken
            {
                AppUserId = user.Id,
                TokenHash = Hash(rawToken),
                CreatedAt = now,
                ExpiresAt = expiresAt,
                IsPersistent = isPersistent,
                DeviceLabel = Truncate(deviceLabel, 256)
            });

            await _db.SaveChangesAsync(cancellationToken);

            return new RefreshOutcome(true, rawToken, user, isPersistent, expiresAt);
        }

        /// <summary>
        /// Validates the presented token and rotates it. The user is identified
        /// from the token row itself, so this deliberately does not depend on
        /// the (possibly already deleted) jwt cookie.
        /// </summary>
        public async Task<RefreshOutcome> ValidateAndRotateAsync(
            string rawToken,
            string? deviceLabel,
            CancellationToken cancellationToken = default)
        {
            var hash = Hash(rawToken);
            var now = DateTime.UtcNow;

            var existing = await _db.RefreshTokens
                .Include(t => t.AppUser)
                .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

            if (existing?.AppUser == null || existing.IsExpired(now))
            {
                return RefreshOutcome.Failed();
            }

            if (existing.RevokedAt is { } revokedAt)
            {
                // Anything revoked deliberately - a sign-out, a password reset,
                // a security sweep - is dead the instant it happens. Only a
                // rotation gets the grace window.
                if (existing.RevokedReason != RefreshTokenRevocationReason.Rotated)
                {
                    return RefreshOutcome.Failed();
                }

                if (now - revokedAt > RotationGrace)
                {
                    // A token this old being replayed means the value leaked
                    // (or was restored from a stale backup). Drop every session
                    // for the user and make them sign in again.
                    _logger.LogWarning(
                        "Refresh token reuse detected for user {UserId}; revoking all sessions.",
                        existing.AppUserId);
                    await RevokeAllAsync(existing.AppUserId, RefreshTokenRevocationReason.SecuritySweep, cancellationToken);
                    return RefreshOutcome.Failed();
                }

                // Inside the grace window: another tab rotated this token a
                // moment ago. Hand out a fresh one rather than signing the
                // loser of that race out.
            }
            else
            {
                existing.RevokedAt = now;
                existing.RevokedReason = RefreshTokenRevocationReason.Rotated;
            }

            return await IssueAsync(existing.AppUser, existing.IsPersistent, deviceLabel, cancellationToken);
        }

        /// <summary>Revokes a single session, leaving the user's other devices signed in.</summary>
        public async Task RevokeAsync(
            string rawToken,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var hash = Hash(rawToken);
            var existing = await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == hash && t.AppUserId == userId, cancellationToken);

            if (existing is null) return;

            // Overwrite a Rotated marker too: the token the client is holding
            // may have been rotated seconds ago, and leaving it as Rotated
            // would let the grace window hand out a fresh session to someone
            // who just signed out.
            existing.RevokedAt = DateTime.UtcNow;
            existing.RevokedReason = RefreshTokenRevocationReason.SignedOut;
            await _db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>Revokes every session for the user (sign out everywhere).</summary>
        public async Task RevokeAllAsync(
            string userId,
            RefreshTokenRevocationReason reason = RefreshTokenRevocationReason.SignedOut,
            CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            // Includes already-rotated rows: those are still inside their grace
            // window and would otherwise keep working after a sweep.
            var tokens = await _db.RefreshTokens
                .Where(t => t.AppUserId == userId &&
                            (t.RevokedAt == null || t.RevokedReason == RefreshTokenRevocationReason.Rotated))
                .ToListAsync(cancellationToken);

            if (tokens.Count == 0) return;

            foreach (var token in tokens)
            {
                token.RevokedAt ??= now;
                token.RevokedReason = reason;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Drops this user's dead rows so the table doesn't grow without bound.
        /// Scoped to one user and run on issue, which keeps it cheap enough to
        /// avoid needing a background job.
        /// </summary>
        private async Task PruneAsync(string userId, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var revokedBefore = now - RevokedRetention;

            await _db.RefreshTokens
                .Where(t => t.AppUserId == userId &&
                            (t.ExpiresAt <= now || (t.RevokedAt != null && t.RevokedAt < revokedBefore)))
                .ExecuteDeleteAsync(cancellationToken);
        }

        private static string? Truncate(string? value, int maxLength) =>
            string.IsNullOrWhiteSpace(value)
                ? null
                : value.Length <= maxLength ? value : value[..maxLength];
    }
}
