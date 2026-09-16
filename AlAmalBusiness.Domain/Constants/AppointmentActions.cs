using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Domain.Constants
{
    // Timeline entry kind, the appointment counterpart of FeedbackActions.
    // Stored as int — append, never reorder.
    public enum AppointmentActions
    {
        StatusChanged,
        Assigned,
        Unassigned,
        Note,
        // Moved to a different department because the patient picked the
        // wrong one. Carries the department names it moved between.
        Forwarded
    }
}
