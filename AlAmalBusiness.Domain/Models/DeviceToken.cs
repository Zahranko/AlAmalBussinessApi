using AlAmalBusiness.Domain.Constants;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models
{
    // One phone that should receive push notifications for a user (added
    // 2026-11-22 for the mobile app). Token is the Firebase Cloud Messaging
    // registration token — FCM also carries iOS through APNs, so one column
    // covers both platforms. A token belongs to one device, so registering a
    // token someone else held moves it (a shared phone changes hands).
    public class DeviceToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? UserId { get; set; }
        public User? User { get; set; }

        [Required, MaxLength(512)]
        public string? Token { get; set; }

        // "android" or "ios" — informational, the send is the same.
        [MaxLength(10)]
        public string? Platform { get; set; }

        public DateTime CreatedAt { get; set; } = AppClock.Now;
        public DateTime LastSeenAt { get; set; } = AppClock.Now;
    }
}
