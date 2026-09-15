using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.Appointments
{
    // One row of the appointment procedures or referral sources list — the
    // same { id, name, isActive } shape as the CRM lookup lists, and like
    // them IsActive travels in the create/update body (no status endpoint).
    public class AppointmentListItemDTO
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string? Name { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // One row of the notification emails list.
    public class AppointmentEmailDTO
    {
        public int Id { get; set; }

        [Required, StringLength(256)]
        public string? Email { get; set; }

        // Optional label ("Reception") shown next to the address.
        [StringLength(100)]
        public string? Name { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // {Success, Message, Data}, as every lookup-list service returns.
    public class AppointmentListResponse<T>
    {
        public bool Success { get; set; }
        public bool NotFound { get; set; }
        public string? Message { get; set; }
        public T? Item { get; set; }
    }
}
