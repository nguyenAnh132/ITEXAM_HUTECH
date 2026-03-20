using System.Reflection.Metadata;

namespace ITExam.Services
{
    public class RoutingAPI
    {
        public string LLMApiUrl { get; set; } = string.Empty;
        public string AppScriptUrl { get; set; } = string.Empty;

        public string AdminEmail { get; set; } = string.Empty;

        public string AdminEmailPass { get; set; } = string.Empty;
    }
}
