using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Appointments
{
    // "How did you hear about us" on the appointment page. Retired with
    // IsActive, never deleted, so old requests keep their source.
    public class AppointmentReferralSource
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
