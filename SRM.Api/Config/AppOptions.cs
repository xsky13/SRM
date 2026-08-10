namespace SRM.Api.Config
{
    public sealed class AppOptions
    {
        public const string SectionName = "App";
        public string ServiceName { get; set; } = "Sample api";
        public string EnvironmentLabel { get; set; } = "Development";
    }
}
