using AlAmalBusiness.Application.DTOs.CarChecks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.CarChecks
{
    public interface ICarCheckService
    {
        Task<List<SecurityCarResponse>> GetCarsAsync();
        Task<CarCheckResult<SecurityCarResponse>> CreateCarAsync(SaveSecurityCarDTO dto);
        Task<CarCheckResult<SecurityCarResponse>> UpdateCarAsync(int id, SaveSecurityCarDTO dto);

        Task<CarSheetsResponse?> GetCarSheetsAsync(int carId);
        // todayOnly (SecurityEmp): the current month only.
        Task<CarCheckResult<CarCheckSheetResponse>> OpenSheetAsync(OpenCarCheckSheetDTO dto, string userId, bool todayOnly = false);
        Task<CarCheckGridResponse?> GetGridAsync(int id);
        // A closed month refuses anyone but an Admin.
        // todayOnly (SecurityEmp): only today's day may be saved, and not over a
        // check already done earlier in the same week.
        Task<CarCheckResult<CarCheckGridResponse>> SaveDaysAsync(int id, List<CarCheckDayDTO> days, string userId, bool isAdmin, bool todayOnly = false);
        Task<CarCheckResult<CarCheckSheetResponse>> CloseSheetAsync(int id, string userId);
        Task<CarCheckResult<CarCheckSheetResponse>> ReopenSheetAsync(int id);
        Task<CarCheckResult<bool>> DeleteSheetAsync(int id);

        Task<List<string>> GetEmployeeNamesAsync();
    }
}
