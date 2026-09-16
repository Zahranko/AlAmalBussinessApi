using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Appointments
{
    // Append-only timeline entry, the appointment counterpart of
    // FeedbackHistory.
    public class AppointmentHistory
    {
        [Key]
        public int Id { get; set; }

        public int AppointmentId { get; set; }
        public AppointmentRequest? Appointment { get; set; }

        [Required]
        public string? ActorId { get; set; }
        public User? Actor { get; set; }

        public AppointmentActions Type { get; set; }

        // Only set on StatusChanged entries — lets the timeline read
        // "New -> Scheduled" without recomputing it from neighbouring rows.
        public AppointmentStatus? FromStatus { get; set; }
        public AppointmentStatus? ToStatus { get; set; }

        // Only set on Forwarded entries. Names rather than ids, and
        // snapshots on purpose: renaming a department later must not
        // silently rewrite what the timeline says happened.
        public string? FromDepartmentName { get; set; }
        public string? ToDepartmentName { get; set; }

        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = AppClock.Now;
    }
}
