using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Domain.Constants
{
    // Timeline entry kind, the feedback counterpart of LeadActions.
    public enum FeedbackActions
    {
        StatusChanged,
        Assigned,
        Unassigned,
        Note,
        // Moved to a different department because it was misrouted. Carries
        // the department names it moved between (see FeedbackHistory).
        Forwarded
    }
}
