using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.Services.Interface
{
    public interface ITokenService
    {
        string GenerateToken(string sub, string UserName, IEnumerable<string> roles);

    }
}
