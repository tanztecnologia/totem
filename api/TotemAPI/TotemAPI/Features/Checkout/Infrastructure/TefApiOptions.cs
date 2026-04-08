namespace TotemAPI.Features.Checkout.Infrastructure;

public sealed class TefApiOptions
{
    public const string SectionName = "Tef";

    public string Mode { get; init; } = "Fake";
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
}

