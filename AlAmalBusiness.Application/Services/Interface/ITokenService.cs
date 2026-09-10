using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.Services.Interface
{
    public interface ITokenService
    {
        // fullName rides along as the "name" claim so a client can show the
        // display name straight from the token, without a GET /api/Auth/me;
        // departmentId as AppClaims.DepartmentId, which is what scopes the
        // feedback inbox to the caller's own department.
        string GenerateToken(string sub, string userName, string? fullName, int departmentId, IEnumerable<string> roles);

    }
}
