using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Security.Claims;

namespace ITExam.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _role;

        public AuthorizeRoleAttribute(string role)
        {
            _role = role.ToLower();
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var role = context.HttpContext.User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrWhiteSpace(role) || role.ToLower() != _role)
            {
                context.Result = new RedirectToActionResult("Author", "Auth", null);
            }
        }
    }
}
