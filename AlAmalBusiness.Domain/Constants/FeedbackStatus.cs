using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Domain.Constants
{
    // Where the message sits in the patient-experience team's workflow.
    public enum FeedbackStatus
    {
        New,
        InReview,
        Resolved,
        Archived
    }
}
