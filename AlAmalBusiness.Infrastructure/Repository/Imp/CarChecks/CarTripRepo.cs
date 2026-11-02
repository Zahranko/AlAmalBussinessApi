using AlAmalBusiness.DbContext.Infrastructure;
using AlAmalBusiness.Domain.IRepositories.CarChecks;
using AlAmalBusiness.Domain.Models.CarChecks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Infrastructure.Repository.Imp.CarChecks
{
    public class CarTripRepo : ICarTripRepo
    {
        private readonly AppDbContext _context;

        public CarTripRepo(AppDbContext context)
        {
            _context = context;
        }

        private static IQueryable<CarTripRow> Rows(IQueryable<CarTrip> trips) =>
            trips.AsNoTracking().Select(t => new CarTripRow
            {
                Id = t.Id,
                CarId = t.CarId,
                CarName = t.Car!.Name,
                PlateNumber = t.Car.PlateNumber,
                DriverId = t.DriverId,
                DriverName = t.Driver!.FullName ?? t.Driver.UserName,
                CompanionName = t.CompanionName,
                Destination = t.Destination,
                StartOdometer = t.StartOdometer,
                EndOdometer = t.EndOdometer,
                DepartedAt = t.DepartedAt,
                ReturnedAt = t.ReturnedAt,
                IsSubmitted = t.IsSubmitted,
                SubmittedAt = t.SubmittedAt,
                CreatedAt = t.CreatedAt,
                LastEditedByName = t.LastEditedBy!.FullName ?? t.LastEditedBy.UserName,
                LastEditedAt = t.LastEditedAt
            });

        public async Task<(List<CarTripRow> Items, int Total)> PageAsync(CarTripQuery query)
        {
            var q = _context.CarTrips.AsQueryable();

            if (query.DriverId != null) q = q.Where(t => t.DriverId == query.DriverId);
            if (query.CarId != null) q = q.Where(t => t.CarId == query.CarId);
            if (query.From != null)
            {
                var from = query.From.Value.Date;
                q = q.Where(t => (t.DepartedAt ?? t.CreatedAt) >= from);
            }
            if (query.To != null)
            {
                var end = query.To.Value.Date.AddDays(1);
                q = q.Where(t => (t.DepartedAt ?? t.CreatedAt) < end);
            }

            var total = await q.CountAsync();
            var items = await Rows(q.OrderByDescending(t => t.DepartedAt ?? t.CreatedAt).ThenByDescending(t => t.Id))
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();
            return (items, total);
        }

        public Task<CarTripRow?> GetRowAsync(int id) =>
            Rows(_context.CarTrips.Where(t => t.Id == id)).FirstOrDefaultAsync();

        public Task<CarTrip?> GetAsync(int id) =>
            _context.CarTrips.FirstOrDefaultAsync(t => t.Id == id);

        public Task<bool> IsCarOutAsync(int carId, int excludeTripId) =>
            _context.CarTrips.AnyAsync(t => t.CarId == carId && t.Id != excludeTripId && t.DepartedAt != null && t.ReturnedAt == null);

        public void Add(CarTrip trip) => _context.CarTrips.Add(trip);

        public void Remove(CarTrip trip) => _context.CarTrips.Remove(trip);

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
    }
}
