using System;
using System.Collections.Generic;
using System.Text;

namespace AlAmalBusiness.Application.DTOs.Auth
{
    public  class LoginResult
    {
        public bool IsSuccess { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }
        public static LoginResult Success(string token) => new()
        {
            Token = token,
            IsSuccess = true,
            Message = "Login successful"

        };
        public static LoginResult Fail(string error) => new()
        {
            IsSuccess = false,
            Message = error

        };
    }
}
