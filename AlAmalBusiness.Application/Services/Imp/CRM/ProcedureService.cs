using AlAmalBusiness.Application.DTOs.CRM.LeadManageList;
using AlAmalBusiness.Application.DTOs.CRM.LeadManageList.Response;
using AlAmalBusiness.Application.Services.Interface.CRM;
using AlAmalBusiness.Domain.IRepositories.CRM;
using AlAmalBusiness.Domain.Models.CRM;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.CRM
{
    public class ProcedureService : IProcedureService
    {
        private readonly IProcedureRepo _repo;

        public ProcedureService(IProcedureRepo repo)
        {
            _repo = repo;
        }

        public async Task<ProcedureResponse> CreateProcedureAsync(ProcedureDTO procedure)
        {
            var exists = await _repo.IsNameExist(procedure.Name!, 0);
            if (exists)
            {
                return new ProcedureResponse { Success = false, Message = $"Procedure with name '{procedure.Name}' already exists." };
            }

            var entity = new Procedures { Name = procedure.Name };
            await _repo.CreateAsync(entity);

            return new ProcedureResponse
            {
                Success = true,
                Procedure = new ProcedureDTO { Id = entity.Id, Name = entity.Name, IsActive = entity.IsActive }
            };
        }

        public async Task<IEnumerable<ProcedureDTO>> GetAllProceduresAsync()
        {
            var procedures = await _repo.GetAllAsync();
            return procedures.Select(p => new ProcedureDTO { Id = p.Id, Name = p.Name, IsActive = p.IsActive });
        }

        public async Task<IEnumerable<ProcedureDTO>> GetActiveProceduresAsync()
        {
            var procedures = await _repo.GetActiveAsync();
            return procedures.Select(p => new ProcedureDTO { Id = p.Id, Name = p.Name, IsActive = p.IsActive });
        }

        public async Task<ProcedureResponse> GetProcedureByIdAsync(int procedureId)
        {
            var procedure = await _repo.GetByIdAsync(procedureId);
            if (procedure == null)
            {
                return new ProcedureResponse { Success = false, Message = $"Procedure with ID {procedureId} not found." };
            }

            return new ProcedureResponse
            {
                Success = true,
                Procedure = new ProcedureDTO { Id = procedure.Id, Name = procedure.Name, IsActive = procedure.IsActive }
            };
        }

        public async Task<ProcedureResponse> UpdateProcedureAsync(int procedureId, ProcedureDTO procedure)
        {
            var entity = await _repo.GetByIdAsync(procedureId);
            if (entity == null)
            {
                return new ProcedureResponse { Success = false, Message = $"Procedure with ID {procedureId} not found." };
            }
            if (string.IsNullOrWhiteSpace(procedure.Name))
            {
                return new ProcedureResponse { Success = false, Message = "Procedure name is empty" };
            }

            var nameExists = await _repo.IsNameExist(procedure.Name, procedureId);
            if (nameExists)
            {
                return new ProcedureResponse { Success = false, Message = $"Procedure with name '{procedure.Name}' already exists." };
            }

            entity.Name = procedure.Name;
            entity.IsActive = procedure.IsActive;
            await _repo.UpdateAsync(entity);

            return new ProcedureResponse
            {
                Success = true,
                Procedure = new ProcedureDTO { Id = entity.Id, Name = entity.Name, IsActive = entity.IsActive }
            };
        }
    }
}
