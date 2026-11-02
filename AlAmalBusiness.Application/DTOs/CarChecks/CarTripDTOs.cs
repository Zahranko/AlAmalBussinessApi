using System;
using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.CarChecks
{
    // A trip's fields — the create and update body, and the optional body of
    // depart/return/submit (the row's current fields are saved first, so a
    // driver's last edits ride along with the button they press).
    //
    // DepartedAt/ReturnedAt are read only from an Admin: for everyone else
    // the times come from the depart/return buttons and are stamped by the
    // server. An Admin's update is a full replace, times included.
    public class SaveCarTripDTO
    {
        public int CarId { get; set; }
        public string? CompanionName { get; set; }
        public string? Destination { get; set; }
        public int? StartOdometer { get; set; }
        public int? EndOdometer { get; set; }
        public DateTime? DepartedAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
    }

    public class CarTripResponse
    {
        public int Id { get; set; }
        public int CarId { get; set; }
        public string CarName { get; set; } = string.Empty;
        public string? PlateNumber { get; set; }
        public string DriverId { get; set; } = string.Empty;
        // The signed-in user's full name, or their username when they have none.
        public string? DriverName { get; set; }
        public string? CompanionName { get; set; }
        public string? Destination { get; set; }
        public int? StartOdometer { get; set; }
        public int? EndOdometer { get; set; }
        // End - Start, once both are in.
        public int? Distance { get; set; }
        public DateTime? DepartedAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        // مدة الرحلة: Returned - Departed in whole minutes, once both are in.
        public int? DurationMinutes { get; set; }
        public bool IsSubmitted { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? LastEditedByName { get; set; }
        public DateTime? LastEditedAt { get; set; }
    }

    public class CarTripPageResponse
    {
        public List<CarTripResponse> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    // An active car for the trip form's dropdown.
    public class CarOptionResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PlateNumber { get; set; }
    }
}
