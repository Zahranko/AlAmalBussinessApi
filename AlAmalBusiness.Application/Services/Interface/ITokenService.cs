using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.Services.Interface
{
    public interface ITokenService
    {
        // fullName rides along as the "name" claim so a client can show the
        // display name straight from the token, without a GET /api/Auth/me;
        // departmentId as AppClaims.DepartmentId, the home department every
        // write stamps; and readableDepartmentIds as AppClaims.DepartmentIds,
        // the set that scopes the feedback, appointment and questionnaire
        // screens. Passing the extra grants alone is enough — the home
        // department is folded in here, so no caller has to remember to.
        string GenerateToken(string sub, string userName, string? fullName, int departmentId, IEnumerable<int> readableDepartmentIds, IEnumerable<string> roles);

    }
}
