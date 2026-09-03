using AlAmalBusiness.Domain.Models.CRM;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.CRM
{
    public interface IDoctorRepo
    {
        Task<IEnumerable<Doctors>> GetAllAsync();
        Task<IEnumerable<Doctors>> GetActiveAsync();
        Task<Doctors?> GetByIdAsync(int id);
        Task<Doctors> CreateAsync(Doctors doctor);
        Task<Doctors> UpdateAsync(Doctors doctor);
        Task<bool> IsNameExist(string name, int excludeId);
    }
}
