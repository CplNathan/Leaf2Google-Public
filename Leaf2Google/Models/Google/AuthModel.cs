using Leaf2Google.Models.Generic;

namespace Leaf2Google.Models.Google
{
    // Form Objects
    public class AuthForm
    {
        public string client_id { get; set; } = string.Empty;
        public Uri? redirect_uri { get; set; }
        public string state { get; set; } = string.Empty;

        public List<string> Errors { get; set; } = new List<string>();
    }

    public class AuthPostForm : AuthForm
    {
        public string NissanUsername { get; set; } = string.Empty;
        public string NissanPassword { get; set; } = string.Empty;
    }

    // Database Object
    public class Auth : BaseModel
    {
        public Auth()
        {
            this.AuthState = string.Empty;
            this.ClientId = string.Empty;
        }

        public Guid AuthId { get; set; }

        public virtual Leaf.Leaf? Owner { get; set; }

        public string AuthState { get; set; }

        public Uri? RedirectUri { get; set; }
        public string ClientId { get; set; }

        public Guid? AuthCode { get; set; }

        public DateTime? LastQuery { get; set; }

        public DateTime? LastExecute { get; set; }

        public DateTime? Deleted { get; set; }
    }
}