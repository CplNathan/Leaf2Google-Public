using Leaf2Google.Models.Generic;

namespace Leaf2Google.Models.Google
{
    public class Token : BaseModel
    {
        public Guid TokenId { get; set; }

        public virtual Auth Owner { get; set; }

        public Guid AccessToken { get; set; }

        public Guid RefreshToken { get; set; }

        public DateTime TokenExpires { get; set; }
    }

    public class AccessTokenDto
    {
        public AccessTokenDto(Token InToken)
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
        public RefreshTokenDto(Token InToken) : base(InToken)
        {
            this.refresh_token = InToken.RefreshToken;
        }

        public Guid refresh_token { get; set; }
    }
}