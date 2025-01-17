using System.ComponentModel.DataAnnotations;

namespace SecureCrudAPI.Models
{
    public class LoginModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }

}
