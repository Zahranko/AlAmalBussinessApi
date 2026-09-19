using AlAmalBusiness.Domain.Models.Tickets;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.Tickets
{
    public interface ITicketRepo
    {
        Task<Ticket> CreateAsync(Ticket ticket);

        // Tracked and with no Includes, for the workflow actions to mutate.
        Task<Ticket?> GetByIdAsync(int id);

        Task<TicketDetailRow?> GetDetailAsync(int id);

        Task<(List<TicketListRow> Items, int TotalCount)> PageTicketsAsync(TicketListQuery query);

        // The admin dashboard, aggregated in SQL — see TicketStatsRows.
        Task<TicketStatsRows> GetStatsAsync(TicketStatsQuery query);

        Task SaveChangesAsync();
    }

    public interface ITicketHistoryRepo
    {
        // Queued only — the caller saves it together with whatever it changed
        // on the ticket itself, so a timeline entry can never be written
        // without the change it describes.
        void Add(TicketHistory history);

        Task<List<TicketHistoryRow>> GetByTicketAsync(int ticketId);
    }

    // The three admin lists. Categories and procedures are the same
    // { name, isActive } shape; reasons add the procedure they belong to.
    public interface ITicketCategoryRepo
    {
        Task<List<TicketCategory>> GetAllAsync();
        Task<List<TicketCategory>> GetActiveAsync();
        Task<TicketCategory?> GetByIdAsync(int id);
        Task CreateAsync(TicketCategory category);
        Task SaveChangesAsync();
        Task<bool> IsNameExist(string name, int excludeId);
    }

    public interface ITicketProcedureRepo
    {
        Task<List<TicketProcedure>> GetAllAsync();
        Task<List<TicketProcedure>> GetActiveAsync();
        Task<TicketProcedure?> GetByIdAsync(int id);
        Task CreateAsync(TicketProcedure procedure);
        Task SaveChangesAsync();
        Task<bool> IsNameExist(string name, int excludeId);
    }

    public interface ITicketReasonRepo
    {
        // Procedure included, for its name.
        Task<List<TicketReason>> GetAllAsync();
        Task<List<TicketReason>> GetActiveAsync();
        Task<TicketReason?> GetByIdAsync(int id);
        Task CreateAsync(TicketReason reason);
        Task SaveChangesAsync();
        // Unique per procedure, not globally — see TicketReason.
        Task<bool> IsNameExist(string name, int procedureId, int excludeId);
    }
}
