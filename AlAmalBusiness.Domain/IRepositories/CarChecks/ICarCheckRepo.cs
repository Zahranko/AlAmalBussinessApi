using AlAmalBusiness.Domain.Models.CarChecks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.CarChecks
{
    // A car with how many monthly sheets it has and its newest one.
    public class SecurityCarRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PlateNumber { get; set; }
        public bool IsActive { get; set; }
        public int SheetCount { get; set; }
        public int? LatestSheetId { get; set; }
        public int? LatestYear { get; set; }
        public int? LatestMonth { get; set; }
    }

    public class CarCheckSheetRow
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public string CarName { get; set; } = string.Empty;
        public string? PlateNumber { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int FilledDays { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsClosed { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string? ClosedByName { get; set; }
    }

    public interface ICarCheckRepo
    {
        // ---------- cars ----------
        Task<List<SecurityCarRow>> GetCarsAsync();
        Task<SecurityCar?> GetCarAsync(int id);
        Task<bool> IsCarNameExist(string name, int excludeId);
        Task AddCarAsync(SecurityCar car);

        // ---------- sheets ----------
        // Newest month first.
        Task<List<CarCheckSheetRow>> GetSheetsAsync(int carId);
        Task<CarCheckSheetRow?> GetSheetRowAsync(int id);
        Task<CarCheckSheet?> GetSheetAsync(int id);
        Task<bool> IsSheetOpen(int carId, int year, int month);
        Task AddSheetAsync(CarCheckSheet sheet);
        Task<bool> HasDaysAsync(int sheetId);
        Task DeleteSheetAsync(CarCheckSheet sheet);

        // ---------- days ----------
        // Tracked, for the save to change in place.
        Task<List<CarCheckDay>> GetDaysAsync(int sheetId, bool tracking = false);
        void AddDay(CarCheckDay day);
        void RemoveDay(CarCheckDay day);
        // Names typed before, most used first, for the grid's autocomplete.
        Task<List<string>> GetEmployeeNamesAsync(int take);

        Task SaveChangesAsync();
    }
}
