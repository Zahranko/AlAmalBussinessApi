using AlAmalBusiness.Domain.Models.CarChecks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Domain.IRepositories.CarChecks
{
    public class CarTripRow
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public string CarName { get; set; } = string.Empty;
        public string? PlateNumber { get; set; }
        public string DriverId { get; set; } = string.Empty;
        public string? DriverName { get; set; }
        public string? CompanionName { get; set; }
        public string? Destination { get; set; }
        public int? StartOdometer { get; set; }
        public int? EndOdometer { get; set; }
        public DateTime? DepartedAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public bool IsSubmitted { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? LastEditedByName { get; set; }
        public DateTime? LastEditedAt { get; set; }
    }

    public class CarTripQuery
    {
        // Matched on the day the trip departed (its creation day until then),
        // inclusive. Null = no bound.
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? CarId { get; set; }
        // Set by the service for a driver who may only read their own trips —
        // never by the caller.
        public string? DriverId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 100;
    }

    public interface ICarTripRepo
    {
        // Newest first; drafts are just the newest rows, so they sit on top.
        Task<(List<CarTripRow> Items, int Total)> PageAsync(CarTripQuery query);
        Task<CarTripRow?> GetRowAsync(int id);
        // Tracked, for the service to change in place.
        Task<CarTrip?> GetAsync(int id);
        // Another trip of this car that has left and not come back.
        Task<bool> IsCarOutAsync(int carId, int excludeTripId);
        void Add(CarTrip trip);
        void Remove(CarTrip trip);
        Task SaveChangesAsync();
    }
}
