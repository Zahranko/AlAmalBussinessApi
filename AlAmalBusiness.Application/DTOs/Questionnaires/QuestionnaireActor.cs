namespace AlAmalBusiness.Application.DTOs.Questionnaires
{
    // Who is acting, resolved in the controller from the caller's own
    // validated token — same shape and reasoning as FeedbackActor.
    public record QuestionnaireActor(
        string UserId,
        // Admin only: every department's questionnaires. A QManager is
        // narrowed to DepartmentId below.
        bool CanViewAll,
        // The department on the actor's own token (AppClaims.DepartmentId).
        int DepartmentId);
}
