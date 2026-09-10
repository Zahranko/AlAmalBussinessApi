using System;
using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Feedback.Response
{
    // The detail view: the row, the free-text the patient wrote, and the
    // timeline. Details is deliberately absent from the list row — it is the
    // one big column on the table and no list shows it.
    public class FeedbackDetailResponse : FeedbackListItemResponse
    {
        public string? Details { get; set; }
        public List<FeedbackHistoryResponse> History { get; set; } = new();
    }
}
