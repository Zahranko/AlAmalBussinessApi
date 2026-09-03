using AlAmalBusiness.Domain.Models.CRM;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.CRM
{
    public interface IReferalSourceRepo
    {
        Task<IEnumerable<ReferalSource>> GetAllAsync();
        Task<IEnumerable<ReferalSource>> GetActiveAsync();
        Task<ReferalSource?> GetByIdAsync(int id);
        Task<ReferalSource> CreateAsync(ReferalSource referalSource);
        Task<ReferalSource> UpdateAsync(ReferalSource referalSource);
        Task<bool> IsNameExist(string name, int excludeId);
    }
}
