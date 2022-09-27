// Copyright (c) Nathan Ford. All rights reserved. AuthFormModel.cs

namespace Leaf2Google.Models.Google
{
    public class AuthFormModel : BaseModel
    {
        public string client_id { get; set; } = string.Empty;
        public Uri? redirect_uri { get; set; }
        public string state { get; set; } = string.Empty;
    }

    public class AuthPostFormModel : AuthFormModel
    {
        public string NissanUsername { get; set; } = string.Empty;
        public string NissanPassword { get; set; } = string.Empty;
        public string Captcha { get; set; } = string.Empty;
    }
}