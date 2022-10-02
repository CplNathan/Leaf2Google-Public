using Leaf2Google.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Leaf2Google.Models.Google
{
    // Database Object
    public class AuthModel : BaseModel
    {
        public AuthModel()
        {
            this.AuthState = string.Empty;
            this.ClientId = string.Empty;
        }

        [Key]
        public Guid AuthId { get; set; }

        public virtual Car.CarModel? Owner { get; set; }

        public string AuthState { get; set; }

        public Uri? RedirectUri { get; set; }
        public string ClientId { get; set; }

        public Guid? AuthCode { get; set; }

        public DateTime? LastQuery { get; set; }

        public DateTime? LastExecute { get; set; }

        public DateTime? Deleted { get; set; }
    }
}