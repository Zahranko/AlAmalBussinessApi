namespace AlAmalBusiness.Domain.Constants
{
    // How urgent a maintenance request is (الأهمية), picked by the sender on
    // the public page and the console's form (added 2026-10-26). Normal is
    // the default when none is sent.
    // Stored as int — append, never reorder.
    public enum MaintenancePriority
    {
        Normal,
        Important,
        Urgent
    }
}
