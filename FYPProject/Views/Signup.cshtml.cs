using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FYPProject.Pages
{
    public class SignupModel : PageModel
    {
        [BindProperty]
        public string Username { get; set; } = string.Empty; // empty value until they put a username

        [BindProperty]
        public string Password { get; set; } = string.Empty; // empty value until they put a password

        public IActionResult OnPost()
        {
            // Handle the signup logic (e.g., saving user to a database)
            // Example: Save the user credentials to the database (omitted for brevity)

            return RedirectToPage("/Login"); // Redirect to login page after successful signup
        }
    }
}
