using System.ComponentModel.DataAnnotations;

namespace SecureCrudAPI.Models
{
    public class ResetPasswordModel
    {
        [Required]
        public string Token { get; set; } // Token from the email

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; }
    }
}
