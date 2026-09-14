using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Domain.Constants
{
    public enum LeadActions
    {
        Created,
        Claimed,
        ReOpened,
        Edited,
        FollowUp,
        // Stored as int — append only, never reorder.
        Deleted,
        Restored,


    }
}
