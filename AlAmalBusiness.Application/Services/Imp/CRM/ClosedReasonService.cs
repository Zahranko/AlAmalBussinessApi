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
    public class ClosedReasonService : IClosedReasonService
    {
        private readonly IClosedReasonRepo _repo;

        public ClosedReasonService(IClosedReasonRepo repo)
        {
            _repo = repo;
        }

        public async Task<ClosedReasonResponse> CreateClosedReasonAsync(ClosedReasonDTO closedReason)
        {
            var exists = await _repo.IsNameExist(closedReason.Name!, 0);
            if (exists)
            {
                return new ClosedReasonResponse { Success = false, Message = $"Closed reason with name '{closedReason.Name}' already exists." };
            }

            var entity = new ClosedReason { Name = closedReason.Name };
            await _repo.CreateAsync(entity);

            return new ClosedReasonResponse
            {
                Success = true,
                ClosedReason = new ClosedReasonDTO { Id = entity.Id, Name = entity.Name, IsActive = entity.IsActive }
            };
        }

        public async Task<IEnumerable<ClosedReasonDTO>> GetAllClosedReasonsAsync()
        {
            var reasons = await _repo.GetAllAsync();
            return reasons.Select(r => new ClosedReasonDTO { Id = r.Id, Name = r.Name, IsActive = r.IsActive });
        }

        public async Task<IEnumerable<ClosedReasonDTO>> GetActiveClosedReasonsAsync()
        {
            var reasons = await _repo.GetActiveAsync();
            return reasons.Select(r => new ClosedReasonDTO { Id = r.Id, Name = r.Name, IsActive = r.IsActive });
        }

        public async Task<ClosedReasonResponse> GetClosedReasonByIdAsync(int closedReasonId)
        {
            var reason = await _repo.GetByIdAsync(closedReasonId);
            if (reason == null)
            {
                return new ClosedReasonResponse { Success = false, Message = $"Closed reason with ID {closedReasonId} not found." };
            }

            return new ClosedReasonResponse
            {
                Success = true,
                ClosedReason = new ClosedReasonDTO { Id = reason.Id, Name = reason.Name, IsActive = reason.IsActive }
            };
        }

        public async Task<ClosedReasonResponse> UpdateClosedReasonAsync(int closedReasonId, ClosedReasonDTO closedReason)
        {
            var entity = await _repo.GetByIdAsync(closedReasonId);
            if (entity == null)
            {
                return new ClosedReasonResponse { Success = false, Message = $"Closed reason with ID {closedReasonId} not found." };
            }
            if (string.IsNullOrWhiteSpace(closedReason.Name))
            {
                return new ClosedReasonResponse { Success = false, Message = "Closed reason name is empty" };
            }

            var nameExists = await _repo.IsNameExist(closedReason.Name, closedReasonId);
            if (nameExists)
            {
                return new ClosedReasonResponse { Success = false, Message = $"Closed reason with name '{closedReason.Name}' already exists." };
            }

            entity.Name = closedReason.Name;
            entity.IsActive = closedReason.IsActive;
            await _repo.UpdateAsync(entity);

            return new ClosedReasonResponse
            {
                Success = true,
                ClosedReason = new ClosedReasonDTO { Id = entity.Id, Name = entity.Name, IsActive = entity.IsActive }
            };
        }
    }
}
