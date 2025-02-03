using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace FYPProject.Attributes
{
    public class AdminOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;

            // 🔹 Retrieve Role_Status from session (stored as string)
            string roleStatusStr = httpContext.Session.GetString("UserRole");

            // 🔍 Debugging log
            Console.WriteLine($"[DEBUG] AdminOnly - Session Role_Status: {(string.IsNullOrEmpty(roleStatusStr) ? "NULL" : roleStatusStr)}");

            // 🔹 If Role_Status is NULL (Guest), redirect to Access Denied
            if (string.IsNullOrEmpty(roleStatusStr))
            {
                Console.WriteLine("[DEBUG] AdminOnly - Redirecting Guest to /AccessDenied");
                context.Result = new RedirectToActionResult("AccessDenied", "Error", null);
                return;
            }

            // 🔹 Convert Role_Status to integer
            if (!int.TryParse(roleStatusStr, out int roleStatus))
            {
                Console.WriteLine("[DEBUG] AdminOnly - Invalid Role_Status, redirecting to /AccessDenied");
                context.Result = new RedirectToActionResult("AccessDenied", "Error", null);
                return;
            }

            // 🔹 If Role_Status is NOT 1 (Admin), redirect to Access Denied
            if (roleStatus != 1)
            {
                Console.WriteLine("[DEBUG] AdminOnly - Non-Admin detected, redirecting to /AccessDenied");
                context.Result = new RedirectToActionResult("AccessDenied", "Error", null);
                return;
            }

            // ✅ Allow admin to proceed
            Console.WriteLine("[DEBUG] AdminOnly - Access Granted");
            base.OnActionExecuting(context);
        }
    }
}
