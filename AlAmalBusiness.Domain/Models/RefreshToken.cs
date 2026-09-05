using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models
{
    // A long-lived credential that buys a fresh 15-minute access token, so a
    // day's work doesn't stop every quarter of an hour to log in again.
    //
    // Only the SHA-256 hash of the token is stored: the raw value exists in
    // the client's cookie and nowhere else, so a copy of this table grants
    // nobody a session. Rows are kept (not deleted) once used, because the
    // chain is what lets a replayed token be spotted — see IRefreshTokenRepo.
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public User? User { get; set; }

        // Base64 of the SHA-256 of the raw token: fixed 44 characters.
        [Required]
        [StringLength(64)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }

        // Set when the token is rotated away or the session is signed out.
        public DateTime? RevokedAt { get; set; }

        // The token issued in its place, so a whole rotation chain can be
        // revoked at once when an already-used token turns up again.
        [StringLength(64)]
        public string? ReplacedByHash { get; set; }

        public bool IsActive(DateTime nowUtc) => RevokedAt == null && ExpiresAt > nowUtc;
    }
}
