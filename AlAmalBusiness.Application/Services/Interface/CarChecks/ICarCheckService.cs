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
        Task<CarCheckResult<CarCheckSheetResponse>> OpenSheetAsync(OpenCarCheckSheetDTO dto, string userId);
        Task<CarCheckGridResponse?> GetGridAsync(int id);
        // A closed month refuses anyone but an Admin.
        Task<CarCheckResult<CarCheckGridResponse>> SaveDaysAsync(int id, List<CarCheckDayDTO> days, string userId, bool isAdmin);
        Task<CarCheckResult<CarCheckSheetResponse>> CloseSheetAsync(int id, string userId);
        Task<CarCheckResult<CarCheckSheetResponse>> ReopenSheetAsync(int id);
        Task<CarCheckResult<bool>> DeleteSheetAsync(int id);

        Task<List<string>> GetEmployeeNamesAsync();
    }
}
