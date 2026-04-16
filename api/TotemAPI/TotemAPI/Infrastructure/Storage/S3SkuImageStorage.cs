using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace TotemAPI.Infrastructure.Storage;

public sealed class S3SkuImageStorage : ISkuImageStorage
{
    private readonly IAmazonS3 _s3;
    private readonly S3Options _options;

    public S3SkuImageStorage(IOptions<S3Options> options)
    {
        _options = options.Value;
        var config = new AmazonS3Config
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_options.Region),
            ForcePathStyle = _options.ForcePathStyle
        };
        if (!string.IsNullOrWhiteSpace(_options.ServiceUrl))
            config.ServiceURL = _options.ServiceUrl;

        var creds = new BasicAWSCredentials(_options.AccessKey, _options.SecretKey);
        _s3 = new AmazonS3Client(creds, config);
    }

    public async Task<(string Key, string Url)> UploadAsync(
        Guid tenantId,
        Guid skuId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken ct
    )
    {
        var ext = GetExtension(fileName, contentType);
        var key = $"tenants/{tenantId:D}/skus/{skuId:D}/{Guid.NewGuid():N}{ext}";

        var request = new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key,
            InputStream = content,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
        };

        await _s3.PutObjectAsync(request, ct);
        var url = $"{_options.PublicBaseUrl.TrimEnd('/')}/{_options.Bucket}/{key}";
        return (key, url);
    }

    public async Task DeleteAsync(string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        await _s3.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _options.Bucket,
            Key = key
        }, ct);
    }

    private static string GetExtension(string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName ?? string.Empty)?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(ext) && ext.Length <= 10) return ext;

        return contentType?.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".bin"
        };
    }
}

