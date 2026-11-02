using AlAmalBusiness.Application.DTOs.CarChecks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlAmalBusiness.Application.Services.Interface.CarChecks
{
    // Who is asking, from their token. SeesAll = may read every driver's
    // trips (Admin, SecurityManager); everyone else reads only their own.
    public record CarTripActor(string UserId, bool IsAdmin, bool SeesAll);

    public interface ICarTripService
    {
        Task<List<CarOptionResponse>> GetActiveCarsAsync();
        Task<CarTripPageResponse> PageAsync(CarTripActor actor, DateTime? from, DateTime? to, int? carId, int page, int pageSize);
        Task<CarTripResponse?> GetAsync(int id, CarTripActor actor);

        Task<CarCheckResult<CarTripResponse>> CreateAsync(SaveCarTripDTO dto, CarTripActor actor);
        Task<CarCheckResult<CarTripResponse>> UpdateAsync(int id, SaveCarTripDTO dto, CarTripActor actor);
        Task<CarCheckResult<CarTripResponse>> DepartAsync(int id, SaveCarTripDTO? dto, CarTripActor actor);
        Task<CarCheckResult<CarTripResponse>> ReturnAsync(int id, SaveCarTripDTO? dto, CarTripActor actor);
        Task<CarCheckResult<CarTripResponse>> SubmitAsync(int id, SaveCarTripDTO? dto, CarTripActor actor);
        Task<CarCheckResult<bool>> DeleteAsync(int id, CarTripActor actor);
    }
}
