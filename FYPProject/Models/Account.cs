using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FYPProject.Models
{
    public class Account
    {
        [Key]
        public string User_ID { get; set; } = null!;

        [Required(ErrorMessage = "Please enter your username")]
        [RegularExpression(@"^[a-zA-Z0-9_]{6,24}$", ErrorMessage = "Username must be 6-24 characters long and can only contain letters.")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Please enter your password")]
        [RegularExpression(@"^(?=.[A-Z])(?=.\d)(?=.*[\W_])[A-Za-z\d\W_]{8,24}$
",
            ErrorMessage = "Password must be 8-24 characters long, include at least one uppercase letter, and one number.")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Please enter your email")]
        [EmailAddress(ErrorMessage = "Please include an '@' in the email address.")]
        public string Email { get; set; } = null!;

        public bool Role_Status { get; set; }
    }
}