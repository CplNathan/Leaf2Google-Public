using Leaf2Google.Helpers;
using Leaf2Google.Models.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Leaf2Google.Models.Leaf
{
    // Database Object
    public class Leaf : BaseModel
    {
        public Leaf()
        {
            LeafId = Guid.NewGuid();
            NissanUsername = string.Empty;
            NissanPasswordBytes = Array.Empty<byte>();
            Key = Array.Empty<byte>();
            IV = Array.Empty<byte>();
            PrimaryVin = string.Empty;
        }

        public Leaf(string NissanUsername, string NissanPassword)
        {
            this.LeafId = Guid.NewGuid();

            this.NissanUsername = NissanUsername;

            Tuple<byte[], byte[], byte[]> encryptedPassword = AesEncryption.EncryptStringToBytes(NissanPassword);
            this.Key = encryptedPassword.Item1;
            this.IV = encryptedPassword.Item2;
            this.NissanPasswordBytes = encryptedPassword.Item3;
            this.IsEncrypted = true;
            this.PrimaryVin = string.Empty;
        }

        public Guid LeafId { get; set; }

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

        public bool IsEncrypted { get; set; }

        public string PrimaryVin { get; set; }

        public DateTime? Deleted { get; set; }
    }
}