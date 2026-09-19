using System.Collections.Generic;

namespace AlAmalBusiness.Application.DTOs.Questionnaires
{
    // Who is acting, resolved in the controller from the caller's own
    // validated token — same shape and reasoning as FeedbackActor.
    public record QuestionnaireActor(
        string UserId,
        // Admin only: every department's questionnaires. A QManager is
        // narrowed to DepartmentIds below.
        bool CanViewAll,
        // The department on the actor's own token (AppClaims.DepartmentId),
        // and where a new questionnaire lands when they don't pick one.
        int DepartmentId,
        // Every department the actor may read — see FeedbackActor. A QManager
        // granted several may also create in any of them.
        IReadOnlyList<int> DepartmentIds);
}
