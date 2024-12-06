using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FYPProject.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string Username { get; set; } = string.Empty; // empty value until they put a username

        [BindProperty]
        public string Password { get; set; } = string.Empty; // empty value until they put a password

        public IActionResult OnPost()
        {
            // Validate login logic (checking credentials)
            if (Username == "admin" && Password == "password") // placeholder
            {
                return RedirectToPage("/ControlPanel");
            }
            else
            {
                // Handle invalid login (redirect to login page with error message)
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }
        }
    }
}
