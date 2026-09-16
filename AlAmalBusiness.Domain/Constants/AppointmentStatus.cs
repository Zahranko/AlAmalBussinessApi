using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Domain.Constants
{
    // Where a patient's appointment request sits in the booking team's
    // workflow. The feedback counterpart of this is FeedbackStatus, and the
    // shape is deliberately the same — one open pair, one success terminal,
    // one give-up terminal — so the two dashboards read alike.
    //
    // Scheduled is the success terminal: it is the moment the request became
    // a real appointment, and what CompletedAt is stamped from.
    public enum AppointmentStatus
    {
        New,
        Contacted,
        Scheduled,
        Cancelled
    }
}
