using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ITExam.Models;
using System.Net.Http;
using System.Text.Json;
using ITExam.Services;

namespace ITExam.Filters
{
    public class CheckTokenAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpClientFactory = context.HttpContext.RequestServices.GetService<IHttpClientFactory>();
            var _context = context.HttpContext.RequestServices.GetService<ITExamDbContext>();

            var access_token = context.HttpContext.Request.Cookies["access_token"];

            if (string.IsNullOrEmpty(access_token))
            {
                context.Result = new RedirectToActionResult("Logout", "Auth", null);
                return;
            }

            var user = _context.Users.SingleOrDefault(us => us.AccessToken == access_token);
            if (user == null)
            {
                context.Result = new RedirectToActionResult("Logout", "Auth", null);
                return;
            }

            var client = httpClientFactory.CreateClient();
            var formData = new Dictionary<string, string>
            {
                { "access_token", access_token },
                { "user_id", user.UserId.ToString() },
                { "platform_auth", "itexam" }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, RoutingAPI.CheckTokenUrl)
            {
                Content = new FormUrlEncodedContent(formData)
            };

            var response = client.SendAsync(request).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;

            using var doc = JsonDocument.Parse(responseContent);
            if (doc.RootElement.TryGetProperty("success", out var successElement) && successElement.GetBoolean() == false)
            {
                context.Result = new RedirectToActionResult("Logout", "Auth", null);
            }
        }
    }
}
