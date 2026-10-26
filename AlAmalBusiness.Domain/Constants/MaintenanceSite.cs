namespace AlAmalBusiness.Domain.Constants
{
    // Which building a maintenance request is in (added 2026-10-26), picked
    // from a list on both forms; MaintenanceTicket.Location still says the
    // exact spot (floor, room) in free text. Null on requests raised before
    // the list existed. Stored as int — append, never reorder.
    public enum MaintenanceSite
    {
        Hospital,
        Clinics,
        Administration,
        NursingHousing
    }
}
