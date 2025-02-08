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

            
            string roleStatusStr = httpContext.Session.GetString("UserRole");

            
            Console.WriteLine($"[DEBUG] AdminOnly - Session Role_Status: {(string.IsNullOrEmpty(roleStatusStr) ? "NULL" : roleStatusStr)}");

            
            if (string.IsNullOrEmpty(roleStatusStr))
            {
                Console.WriteLine("[DEBUG] AdminOnly - Redirecting Guest to /AccessDenied");
                context.Result = new RedirectToActionResult("AccessDenied", "Error", null);
                return;
            }

            
            if (!int.TryParse(roleStatusStr, out int roleStatus))
            {
                Console.WriteLine("[DEBUG] AdminOnly - Invalid Role_Status, redirecting to /AccessDenied");
                context.Result = new RedirectToActionResult("AccessDenied", "Error", null);
                return;
            }

            
            if (roleStatus != 1)
            {
                Console.WriteLine("[DEBUG] AdminOnly - Non-Admin detected, redirecting to /AccessDenied");
                context.Result = new RedirectToActionResult("AccessDenied", "Error", null);
                return;
            }

            
            Console.WriteLine("[DEBUG] AdminOnly - Access Granted");
            base.OnActionExecuting(context);
        }
    }
}
