using System;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Leaf2Google.Models;

namespace Leaf2Google.Entities.Car
{
    public class CarModel : BaseModel
    {
        public CarModel()
        {
            CarModelId = Guid.NewGuid();
            NissanUsername = string.Empty;
            NissanPassword = string.Empty;
        }

        public CarModel(string NissanUsername, string NissanPassword)
        {
            CarModelId = Guid.NewGuid();

            this.NissanUsername = NissanUsername;
            this.NissanPassword = NissanPassword;
        }

        [Key] public Guid CarModelId { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        [Display(Name = "Email")]
        public string NissanUsername { get; set; }

        [NotMapped]
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string NissanPassword { get; set; }

        [NotMapped][Required] public string Captcha { get; set; }

        public DateTime? Deleted { get; set; }
    }
}