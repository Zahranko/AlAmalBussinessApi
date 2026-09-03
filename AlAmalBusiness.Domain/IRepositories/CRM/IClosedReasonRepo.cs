using AlAmalBusiness.Domain.Models.CRM;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.CRM
{
    public interface IClosedReasonRepo
    {
        Task<IEnumerable<ClosedReason>> GetAllAsync();
        Task<IEnumerable<ClosedReason>> GetActiveAsync();
        Task<ClosedReason?> GetByIdAsync(int id);
        Task<ClosedReason> CreateAsync(ClosedReason closedReason);
        Task<ClosedReason> UpdateAsync(ClosedReason closedReason);
        Task<bool> IsNameExist(string name, int excludeId);
    }
}
