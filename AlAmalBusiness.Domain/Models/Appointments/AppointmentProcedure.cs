using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Appointments
{
    // The procedures a patient can ask for on the appointment page. Retired
    // with IsActive, never deleted, so old requests keep their procedure.
    public class AppointmentProcedure
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
