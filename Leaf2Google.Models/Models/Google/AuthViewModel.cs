// Copyright (c) Nathan Ford. All rights reserved. AuthFormModel.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace Leaf2Google.Models.Google
{

    public class AuthViewModel : BaseModel
    {
        public string client_id { get; set; } = string.Empty;
        public Uri? redirect_uri { get; set; }
        public string state { get; set; } = string.Empty;
    }

    public class AuthPostFormGoogleModel : AuthViewModel
    {
        public string NissanUsername { get; set; } = string.Empty;
        public string NissanPassword { get; set; } = string.Empty;
    }

    public class AuthPostFormModel : AuthPostFormGoogleModel
    {
        [Required(AllowEmptyStrings = true)] public string Captcha { get; set; } = string.Empty;
    }
}