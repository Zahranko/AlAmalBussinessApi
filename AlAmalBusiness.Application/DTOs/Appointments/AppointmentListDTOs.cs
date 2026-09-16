using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.Appointments
{
    // One row of the appointment referral-sources list — the same
    // { id, name, isActive } shape as the CRM lookup lists, and like them
    // IsActive travels in the create/update body (no status endpoint).
    //
    // This used to have two siblings, an appointment-only Procedures list and
    // a notification-emails list. Both were dropped on 2026-09-16: the
    // department the patient picks now says who owns the request, and the
    // email goes to that department's AManagers instead of to an
    // admin-maintained address list.
    public class AppointmentListItemDTO
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string? Name { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // {Success, Message, Item}, as every lookup-list service returns.
    public class AppointmentListResponse<T>
    {
        public bool Success { get; set; }
        public bool NotFound { get; set; }
        public string? Message { get; set; }
        public T? Item { get; set; }
    }
}
