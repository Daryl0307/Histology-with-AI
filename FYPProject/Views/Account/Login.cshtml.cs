using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FYPProject.Views.Account
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public IActionResult OnPost()
        {
            // To be repalced with SQL query
            string role = string.Empty;

            if (Username == "admin" && Password == "password")
            {
                role = "Admin"; // admin role credential
            }
            else if (Username == "lecturer" && Password == "password")
            {
                role = "Lecturer"; // lecturer credential
            }
            else if (Username == "user" && Password == "password")
            {
                role = "User"; // regular student credential
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return Page();
            }

            // Redirect based on role
            if (role == "Admin" || role == "Lecturer")
            {
                return RedirectToPage("/Histology/Home"); 
            }
            else
            {
                return RedirectToPage("/Histology/Home"); 
            }
        }
    }
}
