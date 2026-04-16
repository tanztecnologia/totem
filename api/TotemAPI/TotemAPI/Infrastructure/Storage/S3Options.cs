namespace TotemAPI.Infrastructure.Storage;

public sealed class S3Options
{
    public const string SectionName = "S3";

    public string Bucket { get; init; } = "totem-local-bucket";
    public string Region { get; init; } = "us-east-1";
    public string AccessKey { get; init; } = "test";
    public string SecretKey { get; init; } = "test";
    public string? ServiceUrl { get; init; } = "http://localhost:4566";
    public bool ForcePathStyle { get; init; } = true;
    public string PublicBaseUrl { get; init; } = "http://localhost:4566";
}

