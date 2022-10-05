using System.ComponentModel.DataAnnotations;

namespace Leaf2Google.Models.Google
{
    public class TokenModel : BaseModel
    {
        [Key]
        public Guid TokenId { get; set; }

        public virtual AuthModel Owner { get; set; }

        public Guid AccessToken { get; set; }

        public Guid RefreshToken { get; set; }

        public DateTime TokenExpires { get; set; }
    }

    public class AccessTokenDto
    {
        public AccessTokenDto(TokenModel InToken)
        {
            this.token_type = "Bearer";
            this.access_token = InToken.AccessToken;
            this.expires_in = (InToken.TokenExpires - DateTime.UtcNow).Seconds;
        }

        public string token_type { get; set; }
        public Guid access_token { get; set; }
        public int expires_in { get; set; }
    }

    public class RefreshTokenDto : AccessTokenDto
    {
        public RefreshTokenDto(TokenModel InToken) : base(InToken)
        {
            this.refresh_token = InToken.RefreshToken;
        }

        public Guid refresh_token { get; set; }
    }
}