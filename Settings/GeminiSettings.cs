namespace swd.Settings
{
    public class GeminiSettings
    {
        public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
        public string Model { get; set; } = "gemini-2.5-flash";
        public string ApiKey { get; set; } = string.Empty;
        public int MaxCandidateProducts { get; set; } = 30;
        public int MaxRecommendations { get; set; } = 5;
    }
}
