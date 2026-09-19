using AlAmalBusiness.Application.DTOs.Tickets;
using AlAmalBusiness.Application.Services.Interface.Tickets;
using AlAmalBusiness.Domain.IRepositories.Tickets;
using AlAmalBusiness.Domain.Models.Tickets;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.Tickets
{
    // The three lists behind the New ticket form. Same rules as the CRM and
    // appointment lookup lists: a unique name, IsActive in the same body, no
    // delete — retiring an entry keeps it on the tickets that already picked
    // it. A reason's name is unique per procedure rather than globally.
    public class TicketListService : ITicketListService
    {
        private readonly ITicketCategoryRepo _categories;
        private readonly ITicketProcedureRepo _procedures;
        private readonly ITicketReasonRepo _reasons;

        public TicketListService(ITicketCategoryRepo categories, ITicketProcedureRepo procedures, ITicketReasonRepo reasons)
        {
            _categories = categories;
            _procedures = procedures;
            _reasons = reasons;
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

        // ---------- reasons ----------

        public async Task<List<TicketReasonItemDTO>> GetReasonsAsync() =>
            (await _reasons.GetAllAsync()).Select(ToReasonDto).ToList();

        public async Task<TicketListResponse<TicketReasonItemDTO>> CreateReasonAsync(TicketReasonItemDTO dto)
        {
            var name = Clean(dto.Name);
            if (name == null) return Failed<TicketReasonItemDTO>("Reason name is empty.");

            var procedure = await _procedures.GetByIdAsync(dto.ProcedureId);
            if (procedure == null || !procedure.IsActive)
                return Failed<TicketReasonItemDTO>("Pick an active procedure for this reason.");

            if (await _reasons.IsNameExist(name, procedure.Id, 0))
                return Failed<TicketReasonItemDTO>($"Reason '{name}' already exists for this procedure.");

            var entity = new TicketReason { Name = name, IsActive = dto.IsActive, ProcedureId = procedure.Id };
            await _reasons.CreateAsync(entity);
            entity.Procedure = procedure;
            return Ok(ToReasonDto(entity));
        }

        public async Task<TicketListResponse<TicketReasonItemDTO>> UpdateReasonAsync(int id, TicketReasonItemDTO dto)
        {
            var entity = await _reasons.GetByIdAsync(id);
            if (entity == null) return Missing<TicketReasonItemDTO>($"Reason with ID {id} not found.");

            var name = Clean(dto.Name);
            if (name == null) return Failed<TicketReasonItemDTO>("Reason name is empty.");

            var procedure = await _procedures.GetByIdAsync(dto.ProcedureId);
            // Keeping a reason under the procedure it already belongs to is
            // allowed even once that procedure is retired (renaming it, say);
            // moving it under a retired one is not.
            if (procedure == null || (!procedure.IsActive && procedure.Id != entity.ProcedureId))
                return Failed<TicketReasonItemDTO>("Pick an active procedure for this reason.");

            if (await _reasons.IsNameExist(name, procedure.Id, id))
                return Failed<TicketReasonItemDTO>($"Reason '{name}' already exists for this procedure.");

            entity.Name = name;
            entity.IsActive = dto.IsActive;
            entity.ProcedureId = procedure.Id;
            entity.Procedure = procedure;
            await _reasons.SaveChangesAsync();
            return Ok(ToReasonDto(entity));
        }

        // ---------- helpers ----------

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static TicketListItemDTO ToDto(int id, string name, bool isActive) =>
            new() { Id = id, Name = name, IsActive = isActive };

        private static TicketReasonItemDTO ToReasonDto(TicketReason reason) => new()
        {
            Id = reason.Id,
            Name = reason.Name,
            IsActive = reason.IsActive,
            ProcedureId = reason.ProcedureId,
            ProcedureName = reason.Procedure?.Name
        };

        private static TicketListResponse<T> Ok<T>(T item) => new() { Success = true, Item = item };

        private static TicketListResponse<T> Failed<T>(string message) => new() { Success = false, Message = message };

        private static TicketListResponse<T> Missing<T>(string message) =>
            new() { Success = false, NotFound = true, Message = message };
    }
}
