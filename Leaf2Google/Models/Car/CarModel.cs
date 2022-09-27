using Leaf2Google.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Leaf2Google.Models.Car
{
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
            this.CarModelId = Guid.NewGuid();

            this.NissanUsername = NissanUsername;

            Tuple<byte[], byte[], byte[]> encryptedPassword = AesEncryption.EncryptStringToBytes(NissanPassword);
            this.Key = encryptedPassword.Item1;
            this.IV = encryptedPassword.Item2;
            this.NissanPasswordBytes = encryptedPassword.Item3;
            this.IsEncrypted = true;
            // Todo MFA/2FA Security Key
        }

        [Key]
        public Guid CarModelId { get; set; }

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
            get
            {
                return IsEncrypted ? AesEncryption.DecryptStringFromBytes(NissanPasswordBytes, Key, IV) : Encoding.UTF8.GetString(NissanPasswordBytes);
            }
            set
            {
                NissanPasswordBytes = IsEncrypted ? AesEncryption.EncryptStringToBytes(value, Key, IV) : Encoding.UTF8.GetBytes(value);
            }
        }

        [NotMapped]
        [Required]
        public string Captcha { get; set; }

        public bool IsEncrypted { get; set; }

        public DateTime? Deleted { get; set; }
    }
}