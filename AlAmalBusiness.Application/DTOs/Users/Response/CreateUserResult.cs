using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.DTOs.Users.Response
{
    public class CreateUserResult
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
    }
}
