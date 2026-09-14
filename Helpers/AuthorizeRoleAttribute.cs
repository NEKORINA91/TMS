using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TMS.Helpers;

namespace TMS.Helpers
{
    public class AuthorizeRoleAttribute : ActionFilterAttribute
    {
        private readonly string[] _roles;

        public AuthorizeRoleAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.Session.GetSessionUser();

            if (user == null)
            {
                context.Result = new RedirectResult("/Auth/Login");
                return;
            }

            bool allowed = false;
            foreach (var role in _roles)
            {
                if (user.Role == role) { allowed = true; break; }
            }

            if (!allowed)
                context.Result = new RedirectResult("/Auth/AccessDenied");

            base.OnActionExecuting(context);
        }
    }
}
