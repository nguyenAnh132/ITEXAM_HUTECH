using System.Reflection.Metadata;

namespace ITExam.Services
{
    public class RoutingAPI
    {
        //Các thuộc tính này được lưu trong secrets
        public string BaseUrl { get; set; } = string.Empty;
        public string LLMApiUrl { get; set; } = string.Empty;
        public string AppScriptUrl { get; set; } = string.Empty;


        public string Login => $"{BaseUrl}auth/login";
        public string GetSubjectUrl => $"{BaseUrl}education/get-subjects";
        public string CheckTokenUrl => $"{BaseUrl}auth/check-token";
    }
}
