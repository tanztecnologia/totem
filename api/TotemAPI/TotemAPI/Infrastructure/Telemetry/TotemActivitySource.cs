using System.Diagnostics;

namespace TotemAPI.Infrastructure.Telemetry;

public static class TotemActivitySource
{
    public const string ServiceName = "TotemAPI";
    public const string ServiceVersion = "1.0.0";

    public static readonly ActivitySource Instance = new(ServiceName, ServiceVersion);
}
