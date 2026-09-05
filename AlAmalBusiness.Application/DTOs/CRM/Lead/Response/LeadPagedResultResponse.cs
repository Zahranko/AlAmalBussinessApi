using AlAmalBusiness.Application.DTOs.CRM.Lead;

namespace AlAmalBusiness.Application.DTOs.CRM.Lead.Response
{
    // GetPaged's response envelope: the page of leads plus the filter that
    // actually produced it (explicit query params, or the cached "last
    // filter" restored on a bare request). The frontend uses Filter/Page to
    // put its search box, dropdowns, and pagination back into the state that
    // produced what's on screen, instead of resetting them to blank on every
    // reload while the data itself stays correctly filtered.
    public class LeadPagedResultResponse
    {
        public List<LeadListItemResponse> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public LeadFilterCacheDTO Filter { get; set; } = new();
    }
}
