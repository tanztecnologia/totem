using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Domain;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record ReplaceSkuStockConsumptionsCommand(
    Guid TenantId,
    Guid SkuId,
    IReadOnlyList<ReplaceSkuStockConsumptionItem> Items
);

public sealed record ReplaceSkuStockConsumptionItem(
    string SourceSkuCode,
    decimal Quantity,
    string Unit
);

public sealed record SkuStockConsumptionResult(
    Guid Id,
    Guid SkuId,
    Guid SourceSkuId,
    string SourceSkuCode,
    decimal QuantityBase
);

public sealed class ReplaceSkuStockConsumptions
{
    public ReplaceSkuStockConsumptions(ISkuRepository skus)
    {
        _skus = skus;
    }

    private readonly ISkuRepository _skus;

    public async Task<IReadOnlyList<SkuStockConsumptionResult>?> HandleAsync(ReplaceSkuStockConsumptionsCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (command.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");

        var target = await _skus.GetByIdAsync(command.TenantId, command.SkuId, ct);
        if (target is null) return null;

        var items = command.Items ?? Array.Empty<ReplaceSkuStockConsumptionItem>();
        var mapped = new List<(SkuStockConsumption Consumption, string SourceSkuCode)>();

        foreach (var item in items)
        {
            var sourceCode = (item.SourceSkuCode ?? string.Empty).Trim();
            if (sourceCode.Length < 1) throw new ArgumentException("SourceSkuCode inválido.");
            if (item.Quantity <= 0) throw new ArgumentException("Quantity inválido.");

            var source = await _skus.GetByCodeAsync(command.TenantId, sourceCode, ct);
            if (source is null) throw new InvalidOperationException($"SKU base não encontrado ({sourceCode}).");
            if (source.StockBaseUnit is null || source.StockOnHandBaseQty is null)
                throw new InvalidOperationException($"Controle de estoque não configurado no SKU base ({sourceCode}).");

            var qtyBase = ConvertToBase(item.Quantity, item.Unit, source.StockBaseUnit.Value);
            mapped.Add(
                (
                    new SkuStockConsumption(
                        Id: Guid.NewGuid(),
                        TenantId: command.TenantId,
                        SkuId: command.SkuId,
                        SourceSkuId: source.Id,
                        QuantityBase: qtyBase
                    ),
                    source.Code
                )
            );
        }

        await _skus.ReplaceStockConsumptionsAsync(command.TenantId, command.SkuId, mapped.Select(x => x.Consumption).ToList().AsReadOnly(), ct);

        var persisted = await _skus.ListStockConsumptionsAsync(command.TenantId, command.SkuId, ct);
        var bySourceId = mapped.ToDictionary(x => x.Consumption.SourceSkuId, x => x.SourceSkuCode);

        return persisted
            .Select(
                x =>
                    new SkuStockConsumptionResult(
                        Id: x.Id,
                        SkuId: x.SkuId,
                        SourceSkuId: x.SourceSkuId,
                        SourceSkuCode: bySourceId.TryGetValue(x.SourceSkuId, out var c) ? c : string.Empty,
                        QuantityBase: x.QuantityBase
                    )
            )
            .ToList()
            .AsReadOnly();
    }

    private static decimal ConvertToBase(decimal quantity, string unit, StockBaseUnit baseUnit)
    {
        var u = (unit ?? string.Empty).Trim().ToLowerInvariant();
        if (u.Length == 0) throw new ArgumentException("Unit inválido.");

        return baseUnit switch
        {
            StockBaseUnit.Unit => u is "un" or "unidade" or "unit" or "u" or "unid"
                ? quantity
                : throw new ArgumentException("Unit inválido para o tipo UNIT."),
            StockBaseUnit.Gram => u is "g" or "gr" or "grama" or "gramas"
                ? quantity
                : u is "kg" or "quilo" or "kilo" or "kilograma" or "kilogramas"
                    ? quantity * 1000m
                    : throw new ArgumentException("Unit inválido para o tipo GRAM."),
            StockBaseUnit.Milliliter => u is "ml" or "mililitro" or "mililitros"
                ? quantity
                : u is "l" or "lt" or "litro" or "litros"
                    ? quantity * 1000m
                    : throw new ArgumentException("Unit inválido para o tipo MILLILITER."),
            _ => throw new ArgumentException("StockBaseUnit inválido.")
        };
    }
}

