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

    // A reason also names the procedure it belongs to.
    public class TicketReasonItemDTO : TicketListItemDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "Pick the procedure this reason belongs to.")]
        public int ProcedureId { get; set; }

        // Read-only on the way out.
        public string? ProcedureName { get; set; }
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
