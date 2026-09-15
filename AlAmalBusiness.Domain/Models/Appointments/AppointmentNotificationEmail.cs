using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Appointments
{
    // An address that receives the "new appointment request" email. Kept as
    // its own admin-maintained list rather than a role, because the people
    // who book appointments (a call centre inbox, say) need not have a
    // console account at all.
    public class AppointmentNotificationEmail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;

        // Optional label so the list reads "Reception — reception@…".
        public string? Name { get; set; }

        // Inactive addresses stay on the list but get nothing.
        public bool IsActive { get; set; } = true;
    }
}
