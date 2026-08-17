using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.DTOs.Users.Response
{
    public class UpdateUserResponse
    {
        public string? Message { get; set; }
        public bool IsSuccess { get; set; }
        public GetUserResponse User { get; set; }

    }
}
