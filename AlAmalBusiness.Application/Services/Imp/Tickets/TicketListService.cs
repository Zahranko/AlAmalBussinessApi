using AlAmalBusiness.Application.DTOs.Tickets;
using AlAmalBusiness.Application.Services.Interface.Tickets;
using AlAmalBusiness.Domain.IRepositories.Tickets;
using AlAmalBusiness.Domain.Models.Tickets;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.Tickets
{
    // The two lists behind the New ticket form. Same rules as the CRM and
    // appointment lookup lists: a unique name, IsActive in the same body, no
    // delete — retiring an entry keeps it on the tickets that already picked
    // it. (The reason was a third list until 2026-09-19; it is free text on
    // the ticket now.)
    public class TicketListService : ITicketListService
    {
        private readonly ITicketCategoryRepo _categories;
        private readonly ITicketProcedureRepo _procedures;

        public TicketListService(ITicketCategoryRepo categories, ITicketProcedureRepo procedures)
        {
            _categories = categories;
            _procedures = procedures;
        }

        // ---------- categories ----------

        public async Task<List<TicketListItemDTO>> GetCategoriesAsync() =>
            (await _categories.GetAllAsync()).Select(c => ToDto(c.Id, c.Name, c.IsActive)).ToList();

        public async Task<TicketListResponse<TicketListItemDTO>> CreateCategoryAsync(TicketListItemDTO dto)
        {
            var name = Clean(dto.Name);
            if (name == null) return Failed<TicketListItemDTO>("Category name is empty.");
            if (await _categories.IsNameExist(name, 0))
                return Failed<TicketListItemDTO>($"Category with name '{name}' already exists.");

            var entity = new TicketCategory { Name = name, IsActive = dto.IsActive };
            await _categories.CreateAsync(entity);
            return Ok(ToDto(entity.Id, entity.Name, entity.IsActive));
        }

        public async Task<TicketListResponse<TicketListItemDTO>> UpdateCategoryAsync(int id, TicketListItemDTO dto)
        {
            var entity = await _categories.GetByIdAsync(id);
            if (entity == null) return Missing<TicketListItemDTO>($"Category with ID {id} not found.");

            var name = Clean(dto.Name);
            if (name == null) return Failed<TicketListItemDTO>("Category name is empty.");
            if (await _categories.IsNameExist(name, id))
                return Failed<TicketListItemDTO>($"Category with name '{name}' already exists.");

            entity.Name = name;
            entity.IsActive = dto.IsActive;
            await _categories.SaveChangesAsync();
            return Ok(ToDto(entity.Id, entity.Name, entity.IsActive));
        }

        // ---------- procedures ----------

        private static TicketProcedureItemDTO ToProcedureDto(TicketProcedure p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            IsActive = p.IsActive,
            AllowsInsurance = p.AllowsInsurance
        };

        public async Task<List<TicketProcedureItemDTO>> GetProceduresAsync() =>
            (await _procedures.GetAllAsync()).Select(ToProcedureDto).ToList();

        public async Task<TicketListResponse<TicketProcedureItemDTO>> CreateProcedureAsync(TicketProcedureItemDTO dto)
        {
            var name = Clean(dto.Name);
            if (name == null) return Failed<TicketProcedureItemDTO>("Procedure name is empty.");
            if (await _procedures.IsNameExist(name, 0))
                return Failed<TicketProcedureItemDTO>($"Procedure with name '{name}' already exists.");

            var entity = new TicketProcedure { Name = name, IsActive = dto.IsActive, AllowsInsurance = dto.AllowsInsurance ?? false };
            await _procedures.CreateAsync(entity);
            return Ok(ToProcedureDto(entity));
        }

        public async Task<TicketListResponse<TicketProcedureItemDTO>> UpdateProcedureAsync(int id, TicketProcedureItemDTO dto)
        {
            var entity = await _procedures.GetByIdAsync(id);
            if (entity == null) return Missing<TicketProcedureItemDTO>($"Procedure with ID {id} not found.");

            var name = Clean(dto.Name);
            if (name == null) return Failed<TicketProcedureItemDTO>("Procedure name is empty.");
            if (await _procedures.IsNameExist(name, id))
                return Failed<TicketProcedureItemDTO>($"Procedure with name '{name}' already exists.");

            entity.Name = name;
            entity.IsActive = dto.IsActive;
            // Left alone when the caller didn't send it — see the DTO.
            if (dto.AllowsInsurance.HasValue) entity.AllowsInsurance = dto.AllowsInsurance.Value;
            await _procedures.SaveChangesAsync();
            return Ok(ToProcedureDto(entity));
        }

        // ---------- helpers ----------

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static TicketListItemDTO ToDto(int id, string name, bool isActive) =>
            new() { Id = id, Name = name, IsActive = isActive };

        private static TicketListResponse<T> Ok<T>(T item) => new() { Success = true, Item = item };

        private static TicketListResponse<T> Failed<T>(string message) => new() { Success = false, Message = message };

        private static TicketListResponse<T> Missing<T>(string message) =>
            new() { Success = false, NotFound = true, Message = message };
    }
}
