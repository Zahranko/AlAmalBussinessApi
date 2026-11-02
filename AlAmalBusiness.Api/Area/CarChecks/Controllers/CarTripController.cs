using AlAmalBusiness.Application.DTOs.CarChecks;
using AlAmalBusiness.Application.Services.Interface.CarChecks;
using AlAmalBusiness.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AlAmalBusiness.Api.Area.CarChecks.Controllers
{
    // Movement orders (أمر حركة): each trip of a hospital car, filled by the
    // driver who takes it. Rules live in CarTripService — a Driver reads and
    // fills only their own trips, a SecurityManager reads everyone's, and a
    // submitted trip is changed or deleted by an Admin alone.
    //
    // Answers: the trip on success, { message } with 400 for a broken rule,
    // 409 for a blocked action, 404 for an id the caller can't read.
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("PerUserLimit")]
    [Authorize(Roles = TripAccess)]
    public class CarTripController : ControllerBase
    {
        private const string TripAccess =
            nameof(AppRoles.Driver) + "," + nameof(AppRoles.SecurityManager) + "," + nameof(AppRoles.Admin);

        private readonly ICarTripService _trips;

        public CarTripController(ICarTripService trips)
        {
            _trips = trips;
        }

        private CarTripActor Actor => new(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            User.IsInRole(nameof(AppRoles.Admin)),
            User.IsInRole(nameof(AppRoles.Admin)) || User.IsInRole(nameof(AppRoles.SecurityManager)));

        // The active cars, for the trip's car dropdown.
        [HttpGet("cars")]
        public async Task<ActionResult<List<CarOptionResponse>>> GetCars() => Ok(await _trips.GetActiveCarsAsync());

        // Newest first; from/to match the day the trip departed (inclusive).
        [HttpGet]
        public async Task<ActionResult<CarTripPageResponse>> GetPaged(
            [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? carId,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 100) =>
            Ok(await _trips.PageAsync(Actor, from, to, carId, page, pageSize));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var trip = await _trips.GetAsync(id, Actor);
            return trip == null ? NotFound() : Ok(trip);
        }

        // Starts a draft trip with the caller as its driver.
        [HttpPost]
        public Task<IActionResult> Create(SaveCarTripDTO dto) => Run(() => _trips.CreateAsync(dto, Actor));

        // The driver's draft, or (Admin) any trip, times included.
        [HttpPut("{id:int}")]
        public Task<IActionResult> Update(int id, SaveCarTripDTO dto) => Run(() => _trips.UpdateAsync(id, dto, Actor));

        // The three buttons. Each may carry the row's current fields, saved
        // first in the same write.
        [HttpPost("{id:int}/depart")]
        public Task<IActionResult> Depart(int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SaveCarTripDTO? dto) =>
            Run(() => _trips.DepartAsync(id, dto, Actor));

        [HttpPost("{id:int}/return")]
        public Task<IActionResult> Return(int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SaveCarTripDTO? dto) =>
            Run(() => _trips.ReturnAsync(id, dto, Actor));

        [HttpPost("{id:int}/submit")]
        public Task<IActionResult> Submit(int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SaveCarTripDTO? dto) =>
            Run(() => _trips.SubmitAsync(id, dto, Actor));

        // The driver's own draft, or (Admin) any trip.
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _trips.DeleteAsync(id, Actor);
            if (result.NotFound) return NotFound();
            if (!result.Success) return Conflict(new { message = result.Error });
            return NoContent();
        }

        private async Task<IActionResult> Run<T>(Func<Task<CarCheckResult<T>>> action)
        {
            var result = await action();
            if (result.NotFound) return NotFound();
            if (result.Conflict) return Conflict(new { message = result.Error });
            if (!result.Success) return BadRequest(new { message = result.Error });
            return Ok(result.Item);
        }
    }
}
