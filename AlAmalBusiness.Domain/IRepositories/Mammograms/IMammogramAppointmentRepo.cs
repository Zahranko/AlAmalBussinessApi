using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.Models.Mammograms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.Mammograms
{
    public interface IMammogramAppointmentRepo
    {
        // Tracked, no Includes — for the workflow writes.
        Task<MammogramAppointment?> GetByIdAsync(int id);

        Task<MammogramAppointmentRow?> GetRowAsync(int id);
        Task<List<MammogramAppointmentHistoryRow>> GetHistoryAsync(int appointmentId);

        // Every appointment starting on that day, in time order — the day
        // calendar, Failed ones included (drawn faded, they don't hold a slot).
        Task<List<MammogramAppointmentRow>> GetDayAsync(DateTime day);

        Task<(List<MammogramAppointmentRow> Items, int TotalCount)> PageAsync(MammogramAppointmentQuery query);

        // Does any appointment that still holds its slot (anything but
        // Failed) overlap [start, end) in that room?
        Task<bool> HasOverlapAsync(DateTime start, DateTime end, int room, int? excludingId);

        // Appointments booked in [from, to), counted per (status, procedures)
        // in one grouped query, plus how many are in the building right now.
        Task<(List<MammogramAppointmentCount> Counts, int ArrivedNow)> GetStatsAsync(DateTime from, DateTime to);

        // Appointments booked in [from, to) with every follow-up entry, for
        // the Excel export.
        Task<(List<MammogramAppointmentRow> Items, List<MammogramAppointmentHistoryRow> FollowUps)> GetForExportAsync(DateTime from, DateTime to);

        // Appointments booked in [from, to), in appointment-time order, for
        // the printed monthly report.
        Task<List<MammogramAppointmentRow>> GetBookedAsync(DateTime from, DateTime to);

        // Doctor names already used on either side, for autocomplete.
        Task<List<string>> GetDoctorNamesAsync();

        void Add(MammogramAppointment appointment);
        void AddHistory(MammogramAppointmentHistory entry);
        Task SaveChangesAsync();
    }

    // The screens' read shape: no Includes, the creator's name joined in.
    public class MammogramAppointmentRow
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneCountryCode { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public MammogramAppointmentStatus Status { get; set; }
        public DateTime AppointmentAt { get; set; }
        public int DurationMinutes { get; set; }
        public int Room { get; set; }
        public MammogramProcedures Procedures { get; set; }
        public string? ReferringDoctor { get; set; }
        public string? ReceivingDoctor { get; set; }
        public string? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ArrivedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }

    public class MammogramAppointmentHistoryRow
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public MammogramAppointmentActions Action { get; set; }
        public MammogramAppointmentStatus? ResultingStatus { get; set; }
        public string? ActorName { get; set; }
        public DateTime? AppointmentAt { get; set; }
        public int? DurationMinutes { get; set; }
        public int? Room { get; set; }
        public bool? CallSucceeded { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MammogramAppointmentCount
    {
        public MammogramAppointmentStatus Status { get; set; }
        public MammogramProcedures Procedures { get; set; }
        public int Count { get; set; }
    }

    // The old dashboard's filter dropdown, plus "mine" (its Mine tab).
    public enum MammogramAppointmentView
    {
        // Appointments today that aren't finished yet, earliest first.
        Today,
        // Booked or followed up — still waiting on an outcome, any day.
        Open,
        // Booked today, whatever day they are for.
        CreatedToday,
        Mine,
        All
    }

    public class MammogramAppointmentQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public MammogramAppointmentView View { get; set; } = MammogramAppointmentView.Today;
        public MammogramAppointmentStatus? Status { get; set; }

        // Name, or phone (leading-zero tolerant).
        public string? Search { get; set; }

        // Set by the service: the caller (for Mine) and Jordan's today.
        public string? UserId { get; set; }
        public DateTime Today { get; set; }
    }
}
