namespace AlAmalBusiness.Application.DTOs.CRM.Lead.Response
{
    // Badge counts for the employee case queue's 5 tabs — computed straight
    // from the DB, independent of GetPaged's filter cache (see
    // LeadController.GetQueueCounts for why that separation matters).
    public class QueueCountsResponse
    {
        public int All { get; set; }
        public int Today { get; set; }
        public int Mine { get; set; }
        public int Unassigned { get; set; }
        public int Closed { get; set; }
    }
}
