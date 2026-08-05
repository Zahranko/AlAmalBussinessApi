using AlAmalBusiness.Application.DTOs;
using AlAmalBusiness.Application.Services.Imp;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.Services.Interface
{
    public interface IUserServices
    {
        public Task<CreateUserResult> CreateUserAsync(CreateUserDTO user);
    }
}
