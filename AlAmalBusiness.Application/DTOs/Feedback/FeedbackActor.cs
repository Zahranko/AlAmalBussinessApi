using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Feedback
{
    // Who is acting, resolved in the controller from the caller's own
    // validated token. Passed into the service rather than having the service
    // reach for IHttpContextAccessor, so the visibility rules stay testable
    // and the Application layer keeps no dependency on the web layer.
    public record FeedbackActor(
        string UserId,
        // True for Admin alone: sees every department's messages. Everyone
        // else, FManager included, is narrowed to DepartmentIds below.
        bool CanViewAll,
        // The department the actor works in (AppClaims.DepartmentId). Reads
        // scope on DepartmentIds, not on this — it is here for the writes
        // that record which department someone belongs to.
        int DepartmentId,
        // Every department the actor may read: their own plus whatever
        // UserDepartments grants them, resolved from the token's
        // AppClaims.DepartmentIds. Never empty for an account that has a
        // department, since that one is always unioned in.
        IReadOnlyList<int> DepartmentIds);

    // The bits the server captures itself on an anonymous public submission.
    public record SubmissionContext(string? IpAddress, string? UserAgent);
}
