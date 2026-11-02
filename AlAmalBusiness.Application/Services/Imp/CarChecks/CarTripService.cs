using AlAmalBusiness.Application.DTOs.CarChecks;
using AlAmalBusiness.Application.Services.Interface.CarChecks;
using AlAmalBusiness.Domain.Constants;
using AlAmalBusiness.Domain.IRepositories.CarChecks;
using AlAmalBusiness.Domain.Models.CarChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Imp.CarChecks
{
    // Movement orders (أمر حركة): one row per trip of a hospital car.
    //
    // The driver is the signed-in user who started the trip. They fill the
    // car, companion, destination and odometer readings, press "depart now"
    // and later "return now" (the server stamps both times — a driver can't
    // type them), then submit. Until then the trip is their draft; after it,
    // nobody but an Admin may change or delete it, and an Admin may change
    // anything, the times included.
    //
    // Reading: a Driver reads only their own trips; SecurityManager and
    // Admin read every trip. Anything the caller may not read answers 404.
    // Changing a draft is its driver's (or an Admin's) alone — a
    // SecurityManager reads other people's drafts but can't touch them.
    public class CarTripService : ICarTripService
    {
        private const int MaxCompanion = 100;
        private const int MaxDestination = 200;
        private const int MaxOdometer = 9_999_999;
        private const int MaxPageSize = 500;

        private readonly ICarTripRepo _repo;
        private readonly ICarCheckRepo _cars;

        public CarTripService(ICarTripRepo repo, ICarCheckRepo cars)
        {
            _repo = repo;
            _cars = cars;
        }

        public async Task<List<CarOptionResponse>> GetActiveCarsAsync() =>
            (await _cars.GetCarsAsync())
                .Where(c => c.IsActive)
                .Select(c => new CarOptionResponse { Id = c.Id, Name = c.Name, PlateNumber = c.PlateNumber })
                .ToList();

        public async Task<CarTripPageResponse> PageAsync(CarTripActor actor, DateTime? from, DateTime? to, int? carId, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

            var (items, total) = await _repo.PageAsync(new CarTripQuery
            {
                From = from,
                To = to,
                CarId = carId,
                DriverId = actor.SeesAll ? null : actor.UserId,
                Page = page,
                PageSize = pageSize
            });

            return new CarTripPageResponse
            {
                Items = items.Select(ToResponse).ToList(),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<CarTripResponse?> GetAsync(int id, CarTripActor actor)
        {
            var row = await _repo.GetRowAsync(id);
            return row == null || !CanSee(row.DriverId, actor) ? null : ToResponse(row);
        }

        public async Task<CarCheckResult<CarTripResponse>> CreateAsync(SaveCarTripDTO dto, CarTripActor actor)
        {
            var trip = new CarTrip { DriverId = actor.UserId, CreatedAt = AppClock.Now };

            var error = await ApplyAsync(trip, dto, actor, isNew: true);
            if (error != null) return Failed(error);

            _repo.Add(trip);
            await _repo.SaveChangesAsync();
            return await OkAsync(trip.Id);
        }

        public async Task<CarCheckResult<CarTripResponse>> UpdateAsync(int id, SaveCarTripDTO dto, CarTripActor actor)
        {
            var (trip, refused) = await LoadForChangeAsync(id, actor);
            if (trip == null) return refused!;

            var error = await ApplyAsync(trip, dto, actor, isNew: false);
            if (error != null) return Failed(error);

            // A submitted trip an Admin edits must still be a whole trip.
            if (trip.IsSubmitted)
            {
                var incomplete = Incomplete(trip);
                if (incomplete != null) return Failed(incomplete);
                trip.LastEditedById = actor.UserId;
                trip.LastEditedAt = AppClock.Now;
            }

            await _repo.SaveChangesAsync();
            return await OkAsync(id);
        }

        // Stamps موعد الانطلاق with the server's clock.
        public async Task<CarCheckResult<CarTripResponse>> DepartAsync(int id, SaveCarTripDTO? dto, CarTripActor actor)
        {
            var (trip, refused) = await LoadForChangeAsync(id, actor);
            if (trip == null) return refused!;
            if (trip.IsSubmitted) return Conflicted(SubmittedMessage);

            if (dto != null)
            {
                var error = await ApplyAsync(trip, dto, actor, isNew: false);
                if (error != null) return Failed(error);
            }

            if (trip.DepartedAt != null) return Conflicted("The departure time is already recorded.");
            if (await _repo.IsCarOutAsync(trip.CarId, trip.Id))
                return Conflicted("This car is already out on another trip that hasn't returned yet.");

            trip.DepartedAt = AppClock.Now;
            await _repo.SaveChangesAsync();
            return await OkAsync(id);
        }

        // Stamps موعد العودة with the server's clock.
        public async Task<CarCheckResult<CarTripResponse>> ReturnAsync(int id, SaveCarTripDTO? dto, CarTripActor actor)
        {
            var (trip, refused) = await LoadForChangeAsync(id, actor);
            if (trip == null) return refused!;
            if (trip.IsSubmitted) return Conflicted(SubmittedMessage);

            if (dto != null)
            {
                var error = await ApplyAsync(trip, dto, actor, isNew: false);
                if (error != null) return Failed(error);
            }

            if (trip.DepartedAt == null) return Conflicted("Record the departure first.");
            if (trip.ReturnedAt != null) return Conflicted("The return time is already recorded.");

            var now = AppClock.Now;
            trip.ReturnedAt = now < trip.DepartedAt ? trip.DepartedAt : now;
            await _repo.SaveChangesAsync();
            return await OkAsync(id);
        }

        // Locks the trip for good — only an Admin changes it after this.
        public async Task<CarCheckResult<CarTripResponse>> SubmitAsync(int id, SaveCarTripDTO? dto, CarTripActor actor)
        {
            var (trip, refused) = await LoadForChangeAsync(id, actor);
            if (trip == null) return refused!;
            if (trip.IsSubmitted) return Conflicted("This trip is already submitted.");

            if (dto != null)
            {
                var error = await ApplyAsync(trip, dto, actor, isNew: false);
                if (error != null) return Failed(error);
            }

            var incomplete = Incomplete(trip);
            if (incomplete != null) return Failed(incomplete);

            trip.IsSubmitted = true;
            trip.SubmittedAt = AppClock.Now;
            await _repo.SaveChangesAsync();
            return await OkAsync(id);
        }

        // A driver may throw away their own draft; a submitted trip is a
        // record and only an Admin removes it.
        public async Task<CarCheckResult<bool>> DeleteAsync(int id, CarTripActor actor)
        {
            var trip = await _repo.GetAsync(id);
            if (trip == null || !CanSee(trip.DriverId, actor)) return new() { NotFound = true };
            if (!actor.IsAdmin)
            {
                if (trip.DriverId != actor.UserId) return new() { Conflict = true, Error = NotYoursMessage };
                if (trip.IsSubmitted) return new() { Conflict = true, Error = SubmittedMessage };
            }

            _repo.Remove(trip);
            await _repo.SaveChangesAsync();
            return new() { Success = true, Item = true };
        }

        // ---------- helpers ----------

        private const string SubmittedMessage = "This trip is submitted — only an admin can change it.";
        private const string NotYoursMessage = "Only the trip's driver can change it.";

        private static bool CanSee(string driverId, CarTripActor actor) =>
            actor.SeesAll || driverId == actor.UserId;

        // The tracked trip if the caller may change it, else the refusal:
        // 404 if they can't read it, 409 if it isn't theirs or is submitted
        // (an Admin passes both).
        private async Task<(CarTrip? Trip, CarCheckResult<CarTripResponse>? Refused)> LoadForChangeAsync(int id, CarTripActor actor)
        {
            var trip = await _repo.GetAsync(id);
            if (trip == null || !CanSee(trip.DriverId, actor)) return (null, new() { NotFound = true });
            if (!actor.IsAdmin)
            {
                if (trip.DriverId != actor.UserId) return (null, Conflicted(NotYoursMessage));
                if (trip.IsSubmitted) return (null, Conflicted(SubmittedMessage));
            }
            return (trip, null);
        }

        // Copies the body onto the trip, or answers why it can't. The times
        // are taken only from an Admin.
        private async Task<string?> ApplyAsync(CarTrip trip, SaveCarTripDTO dto, CarTripActor actor, bool isNew)
        {
            if (dto.CarId != trip.CarId || isNew)
            {
                var car = await _cars.GetCarAsync(dto.CarId);
                if (car == null) return "Pick a car.";
                if (!car.IsActive) return "This car is retired — pick another.";
                // Moving a trip that is out onto a car that is also out
                // would put one car in two places.
                if (!isNew && trip.DepartedAt != null && trip.ReturnedAt == null && await _repo.IsCarOutAsync(dto.CarId, trip.Id))
                    return "That car is already out on another trip.";
            }

            var companion = Blank(dto.CompanionName);
            var destination = Blank(dto.Destination);
            if (companion?.Length > MaxCompanion) return $"The companion's name is longer than {MaxCompanion} characters.";
            if (destination?.Length > MaxDestination) return $"The destination is longer than {MaxDestination} characters.";
            if (dto.StartOdometer is < 0 or > MaxOdometer) return "The current odometer reading isn't valid.";
            if (dto.EndOdometer is < 0 or > MaxOdometer) return "The return odometer reading isn't valid.";
            if (dto.StartOdometer != null && dto.EndOdometer != null && dto.EndOdometer < dto.StartOdometer)
                return "The return odometer reading can't be lower than the current one.";

            if (actor.IsAdmin && !isNew)
            {
                if (dto.ReturnedAt != null && dto.DepartedAt == null) return "A return time needs a departure time.";
                if (dto.ReturnedAt != null && dto.ReturnedAt < dto.DepartedAt) return "The return time can't be before the departure time.";
                if (dto.DepartedAt != null && dto.ReturnedAt == null && dto.DepartedAt != trip.DepartedAt
                    && await _repo.IsCarOutAsync(dto.CarId, trip.Id))
                    return "That car is already out on another trip.";
                trip.DepartedAt = dto.DepartedAt;
                trip.ReturnedAt = dto.ReturnedAt;
            }

            trip.CarId = dto.CarId;
            trip.CompanionName = companion;
            trip.Destination = destination;
            trip.StartOdometer = dto.StartOdometer;
            trip.EndOdometer = dto.EndOdometer;
            return null;
        }

        // What a trip still lacks before it can be submitted.
        private static string? Incomplete(CarTrip t)
        {
            if (t.Destination == null) return "Enter where the car is going.";
            if (t.StartOdometer == null) return "Enter the current odometer reading.";
            if (t.EndOdometer == null) return "Enter the return odometer reading.";
            if (t.DepartedAt == null) return "Record the departure time.";
            if (t.ReturnedAt == null) return "Record the return time.";
            return null;
        }

        private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private async Task<CarCheckResult<CarTripResponse>> OkAsync(int id) =>
            new() { Success = true, Item = ToResponse((await _repo.GetRowAsync(id))!) };

        private static CarCheckResult<CarTripResponse> Failed(string error) => new() { Error = error };
        private static CarCheckResult<CarTripResponse> Conflicted(string error) => new() { Conflict = true, Error = error };

        private static CarTripResponse ToResponse(CarTripRow r) => new()
        {
            Id = r.Id,
            CarId = r.CarId,
            CarName = r.CarName,
            PlateNumber = r.PlateNumber,
            DriverId = r.DriverId,
            DriverName = r.DriverName,
            CompanionName = r.CompanionName,
            Destination = r.Destination,
            StartOdometer = r.StartOdometer,
            EndOdometer = r.EndOdometer,
            Distance = r.StartOdometer != null && r.EndOdometer != null ? r.EndOdometer - r.StartOdometer : null,
            DepartedAt = r.DepartedAt,
            ReturnedAt = r.ReturnedAt,
            DurationMinutes = r.DepartedAt != null && r.ReturnedAt != null
                ? (int)Math.Floor((r.ReturnedAt.Value - r.DepartedAt.Value).TotalMinutes)
                : null,
            IsSubmitted = r.IsSubmitted,
            SubmittedAt = r.SubmittedAt,
            CreatedAt = r.CreatedAt,
            LastEditedByName = r.LastEditedByName,
            LastEditedAt = r.LastEditedAt
        };
    }
}
