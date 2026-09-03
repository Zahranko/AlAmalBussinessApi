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
    public class ReferalSourceService : IReferalSourceService
    {
        private readonly IReferalSourceRepo _repo;

        public ReferalSourceService(IReferalSourceRepo repo)
        {
            _repo = repo;
        }

        public async Task<ReferalSourceResponse> CreateReferalSourceAsync(ReferalSourceDTO referalSource)
        {
            var exists = await _repo.IsNameExist(referalSource.Name!, 0);
            if (exists)
            {
                return new ReferalSourceResponse { Success = false, Message = $"Referral source with name '{referalSource.Name}' already exists." };
            }

            var entity = new ReferalSource { Name = referalSource.Name };
            await _repo.CreateAsync(entity);

            return new ReferalSourceResponse
            {
                Success = true,
                ReferalSource = new ReferalSourceDTO { Id = entity.Id, Name = entity.Name, IsActive = entity.IsActive }
            };
        }

        public async Task<IEnumerable<ReferalSourceDTO>> GetAllReferalSourcesAsync()
        {
            var sources = await _repo.GetAllAsync();
            return sources.Select(r => new ReferalSourceDTO { Id = r.Id, Name = r.Name, IsActive = r.IsActive });
        }

        public async Task<IEnumerable<ReferalSourceDTO>> GetActiveReferalSourcesAsync()
        {
            var sources = await _repo.GetActiveAsync();
            return sources.Select(r => new ReferalSourceDTO { Id = r.Id, Name = r.Name, IsActive = r.IsActive });
        }

        public async Task<ReferalSourceResponse> GetReferalSourceByIdAsync(int referalSourceId)
        {
            var source = await _repo.GetByIdAsync(referalSourceId);
            if (source == null)
            {
                return new ReferalSourceResponse { Success = false, Message = $"Referral source with ID {referalSourceId} not found." };
            }

            return new ReferalSourceResponse
            {
                Success = true,
                ReferalSource = new ReferalSourceDTO { Id = source.Id, Name = source.Name, IsActive = source.IsActive }
            };
        }

        public async Task<ReferalSourceResponse> UpdateReferalSourceAsync(int referalSourceId, ReferalSourceDTO referalSource)
        {
            var entity = await _repo.GetByIdAsync(referalSourceId);
            if (entity == null)
            {
                return new ReferalSourceResponse { Success = false, Message = $"Referral source with ID {referalSourceId} not found." };
            }
            if (string.IsNullOrWhiteSpace(referalSource.Name))
            {
                return new ReferalSourceResponse { Success = false, Message = "Referral source name is empty" };
            }

            var nameExists = await _repo.IsNameExist(referalSource.Name, referalSourceId);
            if (nameExists)
            {
                return new ReferalSourceResponse { Success = false, Message = $"Referral source with name '{referalSource.Name}' already exists." };
            }

            entity.Name = referalSource.Name;
            entity.IsActive = referalSource.IsActive;
            await _repo.UpdateAsync(entity);

            return new ReferalSourceResponse
            {
                Success = true,
                ReferalSource = new ReferalSourceDTO { Id = entity.Id, Name = entity.Name, IsActive = entity.IsActive }
            };
        }
    }
}
