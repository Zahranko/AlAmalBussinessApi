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
        // 2026-11-22: an employee asked for an edit; an admin turned one down.
        // An approved request is recorded as Edited, with the request named.
        EditRequested,
        EditDeclined,


    }
}
