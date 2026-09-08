namespace AlAmalBusiness.Application.DTOs.CRM.Stats
{
    public class EmployeeCaseCountDTO
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;

        // Cases the user created in the requested period, then how those cases
        // currently stand. Success/Closed are the lead's status *now*, not the
        // status it had inside the period.
        public int Count { get; set; }
        public int Success { get; set; }
        public int Closed { get; set; }
    }
}
