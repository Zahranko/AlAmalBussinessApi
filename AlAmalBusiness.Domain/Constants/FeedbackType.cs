using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Domain.Constants
{
    // What the patient is telling us. Drives the card they pick on the public
    // form and the badge in the staff inbox.
    public enum FeedbackType
    {
        Thanks = 1,
        Suggestion = 2,
        Complaint = 3
    }
}
