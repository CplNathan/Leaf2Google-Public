// Copyright (c) Nathan Ford. All rights reserved. AuthFormModel.cs

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Leaf2Google.Models.Google
{
    public enum ResponseState
    {
        BadRequest,
        InvalidCredentials,
        Success
    }

    public enum RequestState
    {
        Initial,
        Final
    }

    public class LoginModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Email address is not a valid format.")]
        public string NissanUsername { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string NissanPassword { get; set; } = string.Empty;

        [Required(AllowEmptyStrings = true)]
        public string Captcha { get; set; } = string.Empty;
    }

    public class CurrentUser : LoginModel
    {
        public bool IsAuthenticated { get; set; }

        public Guid SessionId { get; set; }

        public Dictionary<string, string> Claims { get; set; }
    }

    public class RegisterModel : LoginModel
    {
        public RequestState request { get; set; }

        public string client_id { get; set; } = string.Empty;
        public Uri? redirect_uri { get; set; }
        public string state { get; set; } = string.Empty;
    }

    public class LoginResponse : LoginModel
    {
        public string sessionId { get; set; }
        public string jwtBearer { get; set; }

        public ResponseState message { get; set; }
        public bool success { get; set; }
    }

    public class RegisterResponse : RegisterModel
    {
        public ResponseState message { get; set; }
        public Guid code { get; set; }
        public bool success { get; set; }
    }
}