using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Domain.Constants
{
    // Non-standard claims this app puts on its own access tokens, on top of
    // the JWT registered ones (sub / unique_name / name) and role.
    public static class AppClaims
    {
        // The user's department id. The feedback inbox scopes on it, so it is
        // an authorization input, not decoration — it is read from the signed
        // token rather than the request body for exactly that reason. Like
        // roles, a change to it takes effect on the next refresh (<= 15 min)
        // rather than whenever the browser is next closed.
        public const string DepartmentId = "department_id";
    }
}
