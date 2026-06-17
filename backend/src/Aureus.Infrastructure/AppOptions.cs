namespace Aureus.Infrastructure;

public sealed class AppOptions
{
    public const string SectionName = "App";

    public string BaseUrl { get; set; } = string.Empty;
}
