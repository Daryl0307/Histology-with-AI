using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FYPProject.Pages
{
    [Authorize(Roles = "Admin,Lecturer")] // Restrict access to Admin and Lecturer roles
    public class ControlPanelModel : PageModel
    {
        public string UserName { get; set; } = string.Empty;

        public void OnGet()
        {
            // to be replaced with sql query to retrieve username
            UserName = User.Identity?.Name ?? "Admin"; // Placeholder 
        }
    }
}
