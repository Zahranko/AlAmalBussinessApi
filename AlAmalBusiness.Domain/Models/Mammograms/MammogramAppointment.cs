using AlAmalBusiness.Domain.Constants;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Domain.Models.Mammograms
{
    // One booked slot on the mammogram day calendar (added 2026-10-17, ported
    // from the standalone alamal-appointments app, where it was a "case" on a
    // Customer row). Staff click a half hour on a day and book a patient into
    // one of the two rooms; everything after that is the appointment's
    // follow-up workflow and its timeline.
    //
    // Every appointment has a slot — the old app's slot-less "plain case" was
    // never linked from its own menu and didn't come across. Two appointments
    // may share a time only in different rooms; the rule lives in
    // MammogramAppointmentService.
    public class MammogramAppointment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        // Stored the feedback/register way: "+962" in its own column, the
        // national number digits-only without its leading zero.
        public string PhoneCountryCode { get; set; } = "+962";
        public string PhoneNumber { get; set; } = string.Empty;

        // The booking note — the old app's Description (a ★ on the card).
        public string? Notes { get; set; }

        public MammogramAppointmentStatus Status { get; set; } = MammogramAppointmentStatus.Booked;

        // Jordan time (AppClock), like every business timestamp here.
        public DateTime AppointmentAt { get; set; }
        public int DurationMinutes { get; set; }

        // 1 or 2 — the two mammogram rooms.
        public int Room { get; set; } = 1;

        // Same four procedures the register uses, any mix.
        public MammogramProcedures Procedures { get; set; }

        // Free text, autocompleted from names already used — doctors this
        // system may have no row for.
        public string? ReferringDoctor { get; set; }
        public string? ReceivingDoctor { get; set; }

        public string? CreatedById { get; set; }
        public User? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = AppClock.Now;

        // Stamped by the Arrived action and by Success/Failed, cleared by
        // nothing: they record when it happened, not the current state.
        public DateTime? ArrivedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public List<MammogramAppointmentHistory> History { get; set; } = new();
    }

    // Append-only timeline of an appointment: created, each follow-up and the
    // arrival. A follow-up that moved the slot snapshots the new one, so the
    // timeline still says where it was moved to after a later move.
    public class MammogramAppointmentHistory
    {
        [Key]
        public int Id { get; set; }

        public int AppointmentId { get; set; }
        public MammogramAppointment? Appointment { get; set; }

        public string? ActorId { get; set; }
        public User? Actor { get; set; }

        public MammogramAppointmentActions Action { get; set; }
        public MammogramAppointmentStatus? ResultingStatus { get; set; }

        // Set when this entry booked or moved the slot.
        public DateTime? AppointmentAt { get; set; }
        public int? DurationMinutes { get; set; }
        public int? Room { get; set; }

        // A follow-up call's outcome — reached the patient or not. Null when
        // nobody said; never changes the status.
        public bool? CallSucceeded { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = AppClock.Now;
    }
}
