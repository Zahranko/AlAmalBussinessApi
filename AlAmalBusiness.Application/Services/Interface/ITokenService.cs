using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.Services.Interface
{
    public interface ITokenService
    {
        // fullName rides along as the "name" claim so a client can show the
        // display name straight from the token, without a GET /api/Auth/me.
        string GenerateToken(string sub, string userName, string? fullName, IEnumerable<string> roles);

    }
}
