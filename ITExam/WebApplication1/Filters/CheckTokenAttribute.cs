using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ITExam.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ITExam.Filters
{
    public class CheckTokenAttribute : ActionFilterAttribute
    {
        private readonly bool _redirectToLoginSuccessOnValid;
        private readonly bool _redirectToLogoutOnInvalid;

        /// <summary>
        /// Default behavior keeps the original intent:
        /// - if token/user is invalid => redirect to Logout
        /// - if token/user is valid   => do nothing
        ///
        /// You can override it for public pages (login/index):
        /// - if token/user is valid   => redirect to LoginSuccess...
        /// - if token/user is invalid => do not redirect (let the page show)
        /// </summary>
        public CheckTokenAttribute(bool redirectToLoginSuccessOnValid = false, bool redirectToLogoutOnInvalid = true)
        {
            _redirectToLoginSuccessOnValid = redirectToLoginSuccessOnValid;
            _redirectToLogoutOnInvalid = redirectToLogoutOnInvalid;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var _context = context.HttpContext.RequestServices.GetService<ITExamDbContext>();
            if (_context == null)
            {
                if (_redirectToLogoutOnInvalid)
                    context.Result = new RedirectToActionResult("Logout", "Auth", null);
                return;
            }

            var identity = context.HttpContext.User?.Identity;
            if (identity == null || !identity.IsAuthenticated)
            {
                if (_redirectToLogoutOnInvalid)
                    context.Result = new RedirectToActionResult("Logout", "Auth", null);
                return;
            }

            var userIdStr = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
            {
                if (_redirectToLogoutOnInvalid)
                    context.Result = new RedirectToActionResult("Logout", "Auth", null);
                return;
            }

            var user = _context.Users.AsNoTracking().FirstOrDefault(u => u.UserId == userId);
            if (user == null)
            {
                if (_redirectToLogoutOnInvalid)
                    context.Result = new RedirectToActionResult("Logout", "Auth", null);
                return;
            }

            if (!_redirectToLoginSuccessOnValid)
                return; // token/user is valid, but for protected pages we do nothing

            var role = user.Role?.Trim()?.ToLower();
            // Redirect authenticated users away from public pages like /login and /home.
            context.Result = role == "teacher"
                ? new RedirectToActionResult("LoginSuccessTeacher", "Auth", null)
                : role == "admin"
                    ? new RedirectToActionResult("Index", "Admin", null)
                    : new RedirectToActionResult("LoginSuccessStudent", "Auth", null);
        }
    }
}
