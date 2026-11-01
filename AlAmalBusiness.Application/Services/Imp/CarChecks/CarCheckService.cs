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
    // The security team's daily vehicle checks: a managed list of cars, one
    // sheet per car per month, a row per day with five ✔/✘ checks, notes and
    // who did it. Not department-scoped — every SecurityManager works every
    // car.
    //
    // A SecurityManager may do everything but delete, change the cars list,
    // reopen a closed month or edit one — those are Admin's alone. The cars
    // list and delete are gated on the controller; the closed-month rule
    // lives here because it depends on the sheet.
    public class CarCheckService : ICarCheckService
    {
        private const int MaxNotes = 500;
        private const int MaxEmployeeName = 100;

        private readonly ICarCheckRepo _repo;

        public CarCheckService(ICarCheckRepo repo)
        {
            _repo = repo;
        }

        // ---------- cars ----------

        public async Task<List<SecurityCarResponse>> GetCarsAsync() =>
            (await _repo.GetCarsAsync()).Select(ToResponse).ToList();

        public async Task<CarCheckResult<SecurityCarResponse>> CreateCarAsync(SaveSecurityCarDTO dto)
        {
            var name = dto.Name?.Trim();
            if (string.IsNullOrEmpty(name)) return Failed<SecurityCarResponse>("Enter the car's name.");
            if (await _repo.IsCarNameExist(name, 0)) return Conflicted<SecurityCarResponse>("A car with this name already exists.");

            var car = new SecurityCar
            {
                Name = name,
                PlateNumber = Blank(dto.PlateNumber),
                IsActive = dto.IsActive,
                CreatedAt = AppClock.Now
            };
            await _repo.AddCarAsync(car);
            return Ok(await CarResponseAsync(car.Id));
        }

        public async Task<CarCheckResult<SecurityCarResponse>> UpdateCarAsync(int id, SaveSecurityCarDTO dto)
        {
            var car = await _repo.GetCarAsync(id);
            if (car == null) return Missing<SecurityCarResponse>();

            var name = dto.Name?.Trim();
            if (string.IsNullOrEmpty(name)) return Failed<SecurityCarResponse>("Enter the car's name.");
            if (await _repo.IsCarNameExist(name, id)) return Conflicted<SecurityCarResponse>("A car with this name already exists.");

            car.Name = name;
            car.PlateNumber = Blank(dto.PlateNumber);
            car.IsActive = dto.IsActive;
            await _repo.SaveChangesAsync();
            return Ok(await CarResponseAsync(id));
        }

        private async Task<SecurityCarResponse> CarResponseAsync(int id) =>
            ToResponse((await _repo.GetCarsAsync()).First(c => c.Id == id));

        // ---------- sheets ----------

        public async Task<CarSheetsResponse?> GetCarSheetsAsync(int carId)
        {
            var car = (await _repo.GetCarsAsync()).FirstOrDefault(c => c.Id == carId);
            if (car == null) return null;

            return new CarSheetsResponse
            {
                Car = ToResponse(car),
                Sheets = (await _repo.GetSheetsAsync(carId)).Select(ToResponse).ToList()
            };
        }

        public async Task<CarCheckResult<CarCheckSheetResponse>> OpenSheetAsync(OpenCarCheckSheetDTO dto, string userId)
        {
            if (dto.Month is < 1 or > 12) return Failed<CarCheckSheetResponse>("Month must be between 1 and 12.");
            if (dto.Year is < 2000 or > 2100) return Failed<CarCheckSheetResponse>("Year isn't valid.");

            var today = AppClock.Today;
            if (dto.Year * 12 + dto.Month > today.Year * 12 + today.Month)
                return Failed<CarCheckSheetResponse>("That month hasn't started yet.");

            var car = await _repo.GetCarAsync(dto.CarId);
            if (car == null) return Missing<CarCheckSheetResponse>();
            if (!car.IsActive) return Failed<CarCheckSheetResponse>("This car is retired — reactivate it first.");

            if (await _repo.IsSheetOpen(dto.CarId, dto.Year, dto.Month))
                return Conflicted<CarCheckSheetResponse>($"{Title(dto.Year, dto.Month)} is already open for this car.");

            var sheet = new CarCheckSheet
            {
                CarId = dto.CarId,
                Year = dto.Year,
                Month = dto.Month,
                CreatedById = userId,
                CreatedAt = AppClock.Now
            };
            await _repo.AddSheetAsync(sheet);
            return Ok(ToResponse((await _repo.GetSheetRowAsync(sheet.Id))!));
        }

        public async Task<CarCheckGridResponse?> GetGridAsync(int id)
        {
            var row = await _repo.GetSheetRowAsync(id);
            if (row == null) return null;
            return await GridAsync(row);
        }

        private async Task<CarCheckGridResponse> GridAsync(CarCheckSheetRow row)
        {
            var filled = (await _repo.GetDaysAsync(row.Id)).ToDictionary(d => d.Day);
            var days = DateTime.DaysInMonth(row.Year, row.Month);

            return new CarCheckGridResponse
            {
                Sheet = ToResponse(row),
                Days = Enumerable.Range(1, days)
                    .Select(day => filled.TryGetValue(day, out var d) ? ToDTO(d) : new CarCheckDayDTO { Day = day })
                    .ToList()
            };
        }

        // The grid's save: every changed day in one request, each a full
        // replace of that day; a day sent with nothing filled is cleared.
        // All or nothing — the rules are lengths and day numbers, so one bad
        // day is a client bug, not a typo to report per row.
        public async Task<CarCheckResult<CarCheckGridResponse>> SaveDaysAsync(int id, List<CarCheckDayDTO> days, string userId, bool isAdmin)
        {
            var sheet = await _repo.GetSheetAsync(id);
            if (sheet == null) return Missing<CarCheckGridResponse>();
            if (sheet.IsClosed && !isAdmin) return Conflicted<CarCheckGridResponse>(ClosedMessage);

            days ??= new();
            var daysInMonth = DateTime.DaysInMonth(sheet.Year, sheet.Month);
            if (days.Count > daysInMonth) return Failed<CarCheckGridResponse>("Too many days in one save.");
            if (days.Select(d => d.Day).Distinct().Count() != days.Count) return Failed<CarCheckGridResponse>("A day was sent twice.");

            foreach (var d in days)
            {
                if (d.Day < 1 || d.Day > daysInMonth) return Failed<CarCheckGridResponse>($"Day {d.Day} isn't in this month.");
                d.Notes = Blank(d.Notes);
                d.EmployeeName = Blank(d.EmployeeName);
                if (d.Notes?.Length > MaxNotes) return Failed<CarCheckGridResponse>($"Day {d.Day}: notes are longer than {MaxNotes} characters.");
                if (d.EmployeeName?.Length > MaxEmployeeName) return Failed<CarCheckGridResponse>($"Day {d.Day}: the name is longer than {MaxEmployeeName} characters.");
            }

            var existing = (await _repo.GetDaysAsync(id, tracking: true)).ToDictionary(d => d.Day);
            var now = AppClock.Now;

            foreach (var d in days)
            {
                existing.TryGetValue(d.Day, out var row);

                if (IsBlank(d))
                {
                    if (row != null) _repo.RemoveDay(row);
                    continue;
                }

                if (row == null)
                {
                    row = new CarCheckDay { SheetId = id, Day = d.Day };
                    _repo.AddDay(row);
                }

                row.EngineOil = d.EngineOil;
                row.BrakeOil = d.BrakeOil;
                row.PowerSteeringOil = d.PowerSteeringOil;
                row.RadiatorWater = d.RadiatorWater;
                row.BodyDamage = d.BodyDamage;
                row.Notes = d.Notes;
                row.EmployeeName = d.EmployeeName;
                row.UpdatedById = userId;
                row.UpdatedAt = now;
            }

            await _repo.SaveChangesAsync();
            return Ok(await GridAsync((await _repo.GetSheetRowAsync(id))!));
        }

        // Ends the month: read-only from here for everyone but an Admin.
        public async Task<CarCheckResult<CarCheckSheetResponse>> CloseSheetAsync(int id, string userId)
        {
            var sheet = await _repo.GetSheetAsync(id);
            if (sheet == null) return Missing<CarCheckSheetResponse>();
            if (sheet.IsClosed) return Conflicted<CarCheckSheetResponse>("This month is already closed.");

            sheet.IsClosed = true;
            sheet.ClosedAt = AppClock.Now;
            sheet.ClosedById = userId;
            await _repo.SaveChangesAsync();
            return Ok(ToResponse((await _repo.GetSheetRowAsync(id))!));
        }

        // Admin only (the controller's gate).
        public async Task<CarCheckResult<CarCheckSheetResponse>> ReopenSheetAsync(int id)
        {
            var sheet = await _repo.GetSheetAsync(id);
            if (sheet == null) return Missing<CarCheckSheetResponse>();
            if (!sheet.IsClosed) return Conflicted<CarCheckSheetResponse>("This month isn't closed.");

            sheet.IsClosed = false;
            sheet.ClosedAt = null;
            sheet.ClosedById = null;
            await _repo.SaveChangesAsync();
            return Ok(ToResponse((await _repo.GetSheetRowAsync(id))!));
        }

        // Only while no day has been filled — a filled sheet is a record.
        public async Task<CarCheckResult<bool>> DeleteSheetAsync(int id)
        {
            var sheet = await _repo.GetSheetAsync(id);
            if (sheet == null) return Missing<bool>();
            if (await _repo.HasDaysAsync(id)) return Conflicted<bool>("Only an empty sheet can be deleted.");

            await _repo.DeleteSheetAsync(sheet);
            return Ok(true);
        }

        public Task<List<string>> GetEmployeeNamesAsync() => _repo.GetEmployeeNamesAsync(50);

        // ---------- helpers ----------

        private const string ClosedMessage = "This month is closed — only an admin can change it.";

        private static bool IsBlank(CarCheckDayDTO d) =>
            d.EngineOil == null && d.BrakeOil == null && d.PowerSteeringOil == null &&
            d.RadiatorWater == null && d.BodyDamage == null &&
            d.Notes == null && d.EmployeeName == null;

        private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static string Title(int year, int month) => $"{month}-{year}";

        private static SecurityCarResponse ToResponse(SecurityCarRow c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            PlateNumber = c.PlateNumber,
            IsActive = c.IsActive,
            SheetCount = c.SheetCount,
            LatestSheetId = c.LatestSheetId,
            LatestYear = c.LatestYear,
            LatestMonth = c.LatestMonth
        };

        private static CarCheckSheetResponse ToResponse(CarCheckSheetRow s) => new()
        {
            Id = s.Id,
            CarId = s.CarId,
            CarName = s.CarName,
            PlateNumber = s.PlateNumber,
            Year = s.Year,
            Month = s.Month,
            Title = Title(s.Year, s.Month),
            DaysInMonth = DateTime.DaysInMonth(s.Year, s.Month),
            FilledDays = s.FilledDays,
            CreatedByName = s.CreatedByName,
            CreatedAt = s.CreatedAt,
            IsClosed = s.IsClosed,
            ClosedAt = s.ClosedAt,
            ClosedByName = s.ClosedByName
        };

        private static CarCheckDayDTO ToDTO(CarCheckDay d) => new()
        {
            Day = d.Day,
            EngineOil = d.EngineOil,
            BrakeOil = d.BrakeOil,
            PowerSteeringOil = d.PowerSteeringOil,
            RadiatorWater = d.RadiatorWater,
            BodyDamage = d.BodyDamage,
            Notes = d.Notes,
            EmployeeName = d.EmployeeName
        };

        private static CarCheckResult<T> Ok<T>(T item) => new() { Success = true, Item = item };
        private static CarCheckResult<T> Failed<T>(string error) => new() { Error = error };
        private static CarCheckResult<T> Conflicted<T>(string error) => new() { Conflict = true, Error = error };
        private static CarCheckResult<T> Missing<T>() => new() { NotFound = true };
    }
}
