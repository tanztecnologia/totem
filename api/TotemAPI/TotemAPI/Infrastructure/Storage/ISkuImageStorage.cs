namespace TotemAPI.Infrastructure.Storage;

public interface ISkuImageStorage
{
    Task<(string Key, string Url)> UploadAsync(
        Guid tenantId,
        Guid skuId,
        string fileName,
        string contentType,
        Stream content,
        CancellationToken ct
    );

    Task DeleteAsync(string key, CancellationToken ct);
}

