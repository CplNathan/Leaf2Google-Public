using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Leaf2Google.Models.Car;
// Database Object

// TODO: Generic Type Class
public class CarModel : BaseModel
{
    public CarModel()
    {
        CarModelId = Guid.NewGuid();
        NissanUsername = string.Empty;
        NissanPasswordBytes = Array.Empty<byte>();
        NissanPassword = string.Empty;
        IsEncrypted = false;
        Key = Array.Empty<byte>();
        IV = Array.Empty<byte>();
    }

    public CarModel(string NissanUsername, string NissanPassword)
    {
        CarModelId = Guid.NewGuid();

        this.NissanUsername = NissanUsername;

        var encryptedPassword = AesEncryption.EncryptStringToBytes(NissanPassword);
        Key = encryptedPassword.Item1;
        IV = encryptedPassword.Item2;
        NissanPasswordBytes = encryptedPassword.Item3;
        IsEncrypted = true;
        // Todo MFA/2FA Security Key
    }

    [Key] public Guid CarModelId { get; set; }

    [Required]
    [DataType(DataType.EmailAddress)]
    [Display(Name = "Email")]
    public string NissanUsername { get; set; }

    public byte[] NissanPasswordBytes { get; set; }

    public byte[] Key { get; set; }

    public byte[] IV { get; set; }

    [NotMapped]
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string NissanPassword
    {
        get => IsEncrypted
            ? AesEncryption.DecryptStringFromBytes(NissanPasswordBytes, Key, IV)
            : Encoding.UTF8.GetString(NissanPasswordBytes);
        set => NissanPasswordBytes =
            IsEncrypted ? AesEncryption.EncryptStringToBytes(value, Key, IV) : Encoding.UTF8.GetBytes(value);
    }

    [NotMapped] [Required] public string Captcha { get; set; }

    public bool IsEncrypted { get; set; }

    public DateTime? Deleted { get; set; }
}