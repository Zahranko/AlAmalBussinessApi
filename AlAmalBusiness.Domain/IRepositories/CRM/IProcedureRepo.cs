using AlAmalBusiness.Domain.Models.CRM;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.CRM
{
    public interface IProcedureRepo
    {
        Task<IEnumerable<Procedures>> GetAllAsync();
        Task<IEnumerable<Procedures>> GetActiveAsync();
        Task<Procedures?> GetByIdAsync(int id);
        Task<Procedures> CreateAsync(Procedures procedure);
        Task<Procedures> UpdateAsync(Procedures procedure);
        Task<bool> IsNameExist(string name, int excludeId);
    }
}
