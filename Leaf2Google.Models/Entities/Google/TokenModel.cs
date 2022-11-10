﻿using System;
using System.ComponentModel.DataAnnotations;
using Leaf2Google.Models;

namespace Leaf2Google.Entities.Google
{

    public class TokenModel : BaseModel
    {
        [Key] public Guid TokenId { get; set; }

        public virtual AuthModel Owner { get; set; }

        public Guid AccessToken { get; set; }

        public Guid RefreshToken { get; set; }

        public DateTime TokenExpires { get; set; }
    }

    public class AccessTokenDto
    {
        public AccessTokenDto(TokenModel InToken)
        {
            token_type = "Bearer";
            access_token = InToken.AccessToken;
            expires_in = (InToken.TokenExpires - DateTime.UtcNow).Seconds;
        }

        public string token_type { get; set; }
        public Guid access_token { get; set; }
        public int expires_in { get; set; }
    }

    public class RefreshTokenDto : AccessTokenDto
    {
        public RefreshTokenDto(TokenModel InToken) : base(InToken)
        {
            refresh_token = InToken.RefreshToken;
        }

        public Guid refresh_token { get; set; }
    }
}