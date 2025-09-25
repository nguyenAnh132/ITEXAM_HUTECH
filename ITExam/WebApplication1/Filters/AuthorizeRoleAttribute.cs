using ITExam.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;

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
            var access_token = context.HttpContext.Request.Cookies["access_token"];
            var _context = context.HttpContext.RequestServices.GetService(typeof(ITExamDbContext)) as ITExamDbContext;
            var user = _context.Users.SingleOrDefault(us => us.AccessToken == access_token);
            if (user == null)
            {
                context.Result = new RedirectToActionResult("Logout", "Auth", null);
                return;
            }
            var role = user.Role;

            if (string.IsNullOrEmpty(role) || role.ToLower() != _role)
            {
                context.Result = new RedirectToActionResult("Author", "Auth", null);
            }
        }
    }
}
