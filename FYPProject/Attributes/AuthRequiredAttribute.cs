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

            
            string roleStatusStr = httpContext.Session.GetString("UserRole");

            
            if (string.IsNullOrEmpty(roleStatusStr))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Error", null);
                return;
            }

            
            base.OnActionExecuting(context);
        }
    }
}
