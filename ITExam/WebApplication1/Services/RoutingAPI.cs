namespace ITExam.Services
{
    public class RoutingAPI
    {
        public static string BaseUrl = "http://157.20.82.3:9000/api/";
        public static string Login => $"{BaseUrl}auth/login";
        public static string GetSubjectUrl => $"{BaseUrl}education/get-subjects";
        public static string CheckTokenUrl => $"{BaseUrl}auth/check-token";
        public static string LLMApiUrl => "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=AIzaSyD95ee0GTpyCTJTBp_fC_NFusxKk017SF0";
    }
}
