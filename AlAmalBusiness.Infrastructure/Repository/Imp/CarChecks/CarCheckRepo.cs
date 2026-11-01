using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories.CarChecks;
using AlAmalBusiness.Domain.Models.CarChecks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.CarChecks
{
    public class CarCheckRepo : ICarCheckRepo
    {
        private readonly AppDbContext _context;

        public CarCheckRepo(AppDbContext context)
        {
            _context = context;
        }

        // ---------- cars ----------

        public Task<List<SecurityCarRow>> GetCarsAsync() =>
            _context.SecurityCars.AsNoTracking()
                .OrderByDescending(c => c.IsActive).ThenBy(c => c.Name)
                .Select(c => new SecurityCarRow
                {
                    Id = c.Id,
                    Name = c.Name,
                    PlateNumber = c.PlateNumber,
                    IsActive = c.IsActive,
                    SheetCount = c.Sheets.Count(),
                    LatestSheetId = c.Sheets.OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).Select(s => (int?)s.Id).FirstOrDefault(),
                    LatestYear = c.Sheets.OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).Select(s => (int?)s.Year).FirstOrDefault(),
                    LatestMonth = c.Sheets.OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).Select(s => (int?)s.Month).FirstOrDefault()
                })
                .ToListAsync();

        public Task<SecurityCar?> GetCarAsync(int id) =>
            _context.SecurityCars.FirstOrDefaultAsync(c => c.Id == id);

        public Task<bool> IsCarNameExist(string name, int excludeId) =>
            _context.SecurityCars.AnyAsync(c => c.Name == name && c.Id != excludeId);

        public async Task AddCarAsync(SecurityCar car)
        {
            _context.SecurityCars.Add(car);
            await _context.SaveChangesAsync();
        }

        // ---------- sheets ----------

        private IQueryable<CarCheckSheetRow> Rows(IQueryable<CarCheckSheet> sheets) =>
            sheets.AsNoTracking().Select(s => new CarCheckSheetRow
            {
                Id = s.Id,
                CarId = s.CarId,
                CarName = s.Car!.Name,
                PlateNumber = s.Car.PlateNumber,
                Year = s.Year,
                Month = s.Month,
                FilledDays = s.Days.Count(),
                CreatedByName = s.CreatedBy!.UserName,
                CreatedAt = s.CreatedAt,
                IsClosed = s.IsClosed,
                ClosedAt = s.ClosedAt,
                ClosedByName = s.ClosedBy!.UserName
            });

        public Task<List<CarCheckSheetRow>> GetSheetsAsync(int carId) =>
            Rows(_context.CarCheckSheets
                    .Where(s => s.CarId == carId)
                    .OrderByDescending(s => s.Year).ThenByDescending(s => s.Month))
                .ToListAsync();

        public Task<CarCheckSheetRow?> GetSheetRowAsync(int id) =>
            Rows(_context.CarCheckSheets.Where(s => s.Id == id)).FirstOrDefaultAsync();

        public Task<CarCheckSheet?> GetSheetAsync(int id) =>
            _context.CarCheckSheets.FirstOrDefaultAsync(s => s.Id == id);

        public Task<bool> IsSheetOpen(int carId, int year, int month) =>
            _context.CarCheckSheets.AnyAsync(s => s.CarId == carId && s.Year == year && s.Month == month);

        public async Task AddSheetAsync(CarCheckSheet sheet)
        {
            _context.CarCheckSheets.Add(sheet);
            await _context.SaveChangesAsync();
        }

        public Task<bool> HasDaysAsync(int sheetId) =>
            _context.CarCheckDays.AnyAsync(d => d.SheetId == sheetId);

        public async Task DeleteSheetAsync(CarCheckSheet sheet)
        {
            _context.CarCheckSheets.Remove(sheet);
            await _context.SaveChangesAsync();
        }

        // ---------- days ----------

        public Task<List<CarCheckDay>> GetDaysAsync(int sheetId, bool tracking = false)
        {
            var q = _context.CarCheckDays.Where(d => d.SheetId == sheetId);
            if (!tracking) q = q.AsNoTracking();
            return q.OrderBy(d => d.Day).ToListAsync();
        }

        public void AddDay(CarCheckDay day) => _context.CarCheckDays.Add(day);

        public void RemoveDay(CarCheckDay day) => _context.CarCheckDays.Remove(day);

        public Task<List<string>> GetEmployeeNamesAsync(int take) =>
            _context.CarCheckDays.AsNoTracking()
                .Where(d => d.EmployeeName != null && d.EmployeeName != "")
                .GroupBy(d => d.EmployeeName!)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(take)
                .ToListAsync();

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}
