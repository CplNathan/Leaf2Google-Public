// Copyright (c) Nathan Ford. All rights reserved. TokenEntity.cs

using Leaf2Google.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;

namespace Leaf2Google.Entities.Google
{

    public class TokenEntity : BaseModel
    {
        [Key] public Guid TokenId { get; set; }

        public virtual AuthEntity Owner { get; set; }

        public Guid RefreshToken { get; set; }
    }

    public class TokenDto
    {
        public string token_type { get; set; } = "Bearer";
    }

    public class AccessTokenDto : TokenDto
    {
        public AccessTokenDto(TokenEntity InToken, JwtSecurityToken jwtToken)
        {
            access_token = new JwtSecurityTokenHandler().WriteToken(jwtToken);
            expires_in = (int)(jwtToken.ValidTo - DateTime.UtcNow).TotalSeconds;
        }

        public string access_token { get; set; }
        public int expires_in { get; set; }
    }

    public class RefreshTokenDto : AccessTokenDto
    {
        public RefreshTokenDto(TokenEntity InToken, JwtSecurityToken jwtToken) : base(InToken, jwtToken)
        {
            refresh_token = InToken.RefreshToken;
        }

        public Guid refresh_token { get; set; }
    }
}
