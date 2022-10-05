using System.ComponentModel.DataAnnotations;
using Leaf2Google.Models.Car;

namespace Leaf2Google.Models.Google;

// Database Object
public class AuthModel : BaseModel
{
    public AuthModel()
    {
        AuthState = string.Empty;
        ClientId = string.Empty;
    }

    [Key] public Guid AuthId { get; set; }

    public virtual CarModel? Owner { get; set; }

    public string AuthState { get; set; }

    public Uri? RedirectUri { get; set; }
    public string ClientId { get; set; }

    public Guid? AuthCode { get; set; }

    public DateTime? LastQuery { get; set; }

    public DateTime? LastExecute { get; set; }

    public DateTime? Deleted { get; set; }
}