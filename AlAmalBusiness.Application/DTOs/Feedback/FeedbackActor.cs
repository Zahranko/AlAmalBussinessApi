using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Feedback
{
    // Who is acting, resolved in the controller from the caller's own
    // validated token. Passed into the service rather than having the service
    // reach for IHttpContextAccessor, so the visibility rules stay testable
    // and the Application layer keeps no dependency on the web layer.
    public record FeedbackActor(
        string UserId,
        // True for Admin and FManager: sees every department's messages.
        // Everyone else is narrowed to DepartmentId below.
        bool CanViewAll,
        // The department the actor's own account belongs to.
        int DepartmentId);

    // The bits the server captures itself on an anonymous public submission.
    public record SubmissionContext(string? IpAddress, string? UserAgent);
}
