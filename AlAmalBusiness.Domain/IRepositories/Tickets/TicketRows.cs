using AlAmalBusiness.Domain.Constants;
using System;

namespace AlAmalBusiness.Domain.IRepositories.Tickets
{
    // Flat row the ticket lists project into straight from SQL — the columns
    // a list shows and nothing else, with related names read through the
    // navigations in the same SELECT. Same reasoning as AppointmentListRow:
    // Include'ing two AspNetUsers rows (password hashes, security stamps),
    // three lookups and the Description column for every row is a lot of
    // network for a dozen scalars.
    public class TicketListRow
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        // Who the ticket is about. A list column, not detail-only: it is the
        // same kind of identifying scrap as the title, and the queue is read
        // by people looking for one person's ticket.
        public string? Name { get; set; }
        public TicketStatus Status { get; set; }
        public TicketType Type { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? ProcedureId { get; set; }
        public string? ProcedureName { get; set; }
        public bool IsInsurance { get; set; }
        public bool IsDelayed { get; set; }
        public string? SourceUrl { get; set; }
        public string? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    // The detail screen's row: the list columns plus the free text. Still a
    // projection — a single ticket needs no whole AspNetUsers rows either.
    public class TicketDetailRow : TicketListRow
    {
        // Free text, possibly several lines — detail only. Since 2026-09-20
        // this is the ticket's only prose: Description was removed and what
        // the extension used to put there (the delete list) now leads the
        // reason.
        public string? Reason { get; set; }
        public string? PatientId { get; set; }
        public string? Resolution { get; set; }
    }

    public class TicketHistoryRow
    {
        public int Id { get; set; }
        public TicketActions Type { get; set; }
        public string? ActorName { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
