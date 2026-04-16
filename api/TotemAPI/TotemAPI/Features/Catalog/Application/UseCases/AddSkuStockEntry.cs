using TotemAPI.Features.Catalog.Application.Abstractions;
using TotemAPI.Features.Catalog.Domain;

namespace TotemAPI.Features.Catalog.Application.UseCases;

public sealed record AddSkuStockEntryCommand(
    Guid TenantId,
    Guid SkuId,
    decimal Quantity,
    string Unit,
    Guid? ActorUserId = null,
    string? Notes = null
);

public sealed class AddSkuStockEntry
{
    public AddSkuStockEntry(ISkuRepository skus)
    {
        _skus = skus;
    }

    private readonly ISkuRepository _skus;

    public async Task<SkuResult?> HandleAsync(AddSkuStockEntryCommand command, CancellationToken ct)
    {
        if (command.TenantId == Guid.Empty) throw new ArgumentException("TenantId inválido.");
        if (command.SkuId == Guid.Empty) throw new ArgumentException("SkuId inválido.");
        if (command.Quantity <= 0) throw new ArgumentException("Quantity inválido.");

        var sku = await _skus.GetByIdAsync(command.TenantId, command.SkuId, ct);
        if (sku is null) return null;

        if (!sku.TracksStock)
            throw new InvalidOperationException("SKU não controla estoque próprio.");

        var baseUnit = sku.StockBaseUnit ?? InferBaseUnit(command.Unit);
        var onHand = sku.StockOnHandBaseQty ?? 0m;
        if (sku.StockBaseUnit is null || sku.StockOnHandBaseQty is null)
        {
            await _skus.UpdateAsync(
                sku with
                {
                    StockBaseUnit = baseUnit,
                    StockOnHandBaseQty = onHand,
                    UpdatedAt = DateTimeOffset.UtcNow,
                },
                ct
            );
            sku = await _skus.GetByIdAsync(command.TenantId, command.SkuId, ct);
            if (sku is null) return null;
        }

        var delta = ConvertToBase(command.Quantity, command.Unit, baseUnit);
        await _skus.AddStockLedgerEntryAsync(new SkuStockLedgerEntry(
            Id: Guid.NewGuid(),
            TenantId: command.TenantId,
            SkuId: command.SkuId,
            DeltaBaseQty: delta,
            StockAfterBaseQty: 0, // calculado pelo repositório
            OriginType: StockLedgerOriginType.ManualEntry,
            OriginId: null,
            Notes: command.Notes,
            ActorUserId: command.ActorUserId,
            CreatedAt: DateTimeOffset.UtcNow
        ), ct);

        var updated = await _skus.GetByIdAsync(command.TenantId, command.SkuId, ct);
        if (updated is null) return null;

        return new SkuResult(
            Id: updated.Id,
            TenantId: updated.TenantId,
            CategoryCode: updated.CategoryCode,
            Code: updated.Code,
            Name: updated.Name,
            PriceCents: updated.PriceCents,
            AveragePrepSeconds: updated.AveragePrepSeconds,
            ImageUrl: updated.ImageUrl,
            NfeCProd: updated.NfeCProd,
            NfeCEan: updated.NfeCEan,
            NfeCfop: updated.NfeCfop,
            NfeUCom: updated.NfeUCom,
            NfeQCom: updated.NfeQCom,
            NfeVUnCom: updated.NfeVUnCom,
            NfeVProd: updated.NfeVProd,
            NfeCEanTrib: updated.NfeCEanTrib,
            NfeUTrib: updated.NfeUTrib,
            NfeQTrib: updated.NfeQTrib,
            NfeVUnTrib: updated.NfeVUnTrib,
            NfeIcmsOrig: updated.NfeIcmsOrig,
            NfeIcmsCst: updated.NfeIcmsCst,
            NfeIcmsModBc: updated.NfeIcmsModBc,
            NfeIcmsVBc: updated.NfeIcmsVBc,
            NfeIcmsPIcms: updated.NfeIcmsPIcms,
            NfeIcmsVIcms: updated.NfeIcmsVIcms,
            NfePisCst: updated.NfePisCst,
            NfePisVBc: updated.NfePisVBc,
            NfePisPPis: updated.NfePisPPis,
            NfePisVPis: updated.NfePisVPis,
            NfeCofinsCst: updated.NfeCofinsCst,
            NfeCofinsVBc: updated.NfeCofinsVBc,
            NfeCofinsPCofins: updated.NfeCofinsPCofins,
            NfeCofinsVCofins: updated.NfeCofinsVCofins,
            TracksStock: updated.TracksStock,
            StockBaseUnit: updated.StockBaseUnit,
            StockOnHandBaseQty: updated.StockOnHandBaseQty,
            IsActive: updated.IsActive
        );
    }

    private static StockBaseUnit InferBaseUnit(string unit)
    {
        var u = (unit ?? string.Empty).Trim().ToLowerInvariant();
        if (u is "g" or "gr" or "grama" or "gramas" or "kg" or "quilo" or "kilo" or "kilograma" or "kilogramas")
            return StockBaseUnit.Gram;
        if (u is "ml" or "mililitro" or "mililitros" or "l" or "lt" or "litro" or "litros")
            return StockBaseUnit.Milliliter;
        return StockBaseUnit.Unit;
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
