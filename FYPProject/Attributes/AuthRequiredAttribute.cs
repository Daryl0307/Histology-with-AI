using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace FYPProject.Attributes
{
    public class AuthRequiredAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;

            // 🔹 Retrieve Role_Status from session (stored as string)
            string roleStatusStr = httpContext.Session.GetString("UserRole");

            // 🔹 If Role_Status is NULL (Guest), redirect to Access Denied
            if (string.IsNullOrEmpty(roleStatusStr))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Error", null);
                return;
            }

            // ✅ If Role_Status is set (0 for Students, 1 for Admins), allow access
            base.OnActionExecuting(context);
        }
    }
}
