using System;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using Leaf2Google.Models;

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
            expires_in = (jwtToken.ValidTo - DateTime.UtcNow).Seconds;
        }

        public string access_token { get; set; }
        public int expires_in { get; set; }
    }

    public class RefreshTokenDto : TokenDto
    {
        public RefreshTokenDto(TokenEntity InToken)
        {
            refresh_token = InToken.RefreshToken;
        }

        public Guid refresh_token { get; set; }
    }
}