using System.ComponentModel.DataAnnotations;

namespace AlAmalBusiness.Application.DTOs.Tickets
{
    // One row of the ticket categories or procedures list — the same
    // { id, name, isActive } shape as every other lookup list, IsActive in
    // the same create/update body (no status endpoint).
    public class TicketListItemDTO
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string? Name { get; set; }

        public bool IsActive { get; set; } = true;
    }

    // A procedure also carries whether a ticket on it may be flagged
    // insurance. Seeded true for "Open invoice" and false for the rest, and
    // editable here so the routing stays the admin's to change rather than
    // something baked into a name comparison.
    public class TicketProcedureItemDTO : TicketListItemDTO
    {
        // Nullable on purpose, unlike the full-replace fields around it: a
        // client that doesn't know about this flag (or a form that only meant
        // to fix a typo in the name) must not silently switch the insurance
        // desk's routing off. Omitted means "leave it as it is"; on create it
        // reads as false.
        public bool? AllowsInsurance { get; set; }
    }

    // {Success, Message, Item}, as every lookup-list service returns.
    public class TicketListResponse<T>
    {
        public bool Success { get; set; }
        public bool NotFound { get; set; }
        public string? Message { get; set; }
        public T? Item { get; set; }
    }
}
