using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FYPProject.Pages
{
    public class ControlPanelModel : PageModel
    {
        public string UserName { get; set; } = string.Empty; // empty value until they put a username

        public void OnGet()
        {
            
            UserName = "Admin"; // placeholder
        }
    }
}
