using Microsoft.Extensions.Logging.Abstractions;
using TotemAPI.Features.Cart.Infrastructure;
using TotemAPI.Features.Catalog.Application.UseCases;
using TotemAPI.Features.Catalog.Domain;
using TotemAPI.Features.Catalog.Infrastructure;
using TotemAPI.Features.Checkout.Application.Abstractions;
using TotemAPI.Features.Checkout.Application.UseCases;
using TotemAPI.Features.Checkout.Domain;
using TotemAPI.Features.Checkout.Infrastructure;
using Xunit;

namespace TotemAPI.Tests;

public sealed class StockDeductionTests
{
    [Fact]
    public async Task ConfirmPayment_DecrementsStock_FromConsumptionMapping()
    {
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var skus = new InMemorySkuRepository();
        var carts = new InMemoryCartRepository();
        var checkout = new InMemoryCheckoutRepository();
        var tef = new ApprovedTefPaymentService();

        var batataId = Guid.NewGuid();
        var porcaoId = Guid.NewGuid();

        await skus.AddAsync(
            new Sku(
                Id: batataId,
                TenantId: tenantId,
                CategoryCode: "00001",
                Code: "00010",
                Name: "Batata (base)",
                PriceCents: 0,
                AveragePrepSeconds: null,
                ImageUrl: null,
                NfeCProd: null,
                NfeCEan: null,
                NfeCfop: null,
                NfeUCom: null,
                NfeQCom: null,
                NfeVUnCom: null,
                NfeVProd: null,
                NfeCEanTrib: null,
                NfeUTrib: null,
                NfeQTrib: null,
                NfeVUnTrib: null,
                NfeIcmsOrig: null,
                NfeIcmsCst: null,
                NfeIcmsModBc: null,
                NfeIcmsVBc: null,
                NfeIcmsPIcms: null,
                NfeIcmsVIcms: null,
                NfePisCst: null,
                NfePisVBc: null,
                NfePisPPis: null,
                NfePisVPis: null,
                NfeCofinsCst: null,
                NfeCofinsVBc: null,
                NfeCofinsPCofins: null,
                NfeCofinsVCofins: null,
                TracksStock: true,
                StockBaseUnit: StockBaseUnit.Gram,
                StockOnHandBaseQty: 10000m,
                IsActive: true,
                CreatedAt: now,
                UpdatedAt: now
            ),
            CancellationToken.None
        );

        await skus.AddAsync(
            new Sku(
                Id: porcaoId,
                TenantId: tenantId,
                CategoryCode: "00001",
                Code: "00011",
                Name: "Porção batata 200g",
                PriceCents: 1500,
                AveragePrepSeconds: null,
                ImageUrl: null,
                NfeCProd: null,
                NfeCEan: null,
                NfeCfop: null,
                NfeUCom: null,
                NfeQCom: null,
                NfeVUnCom: null,
                NfeVProd: null,
                NfeCEanTrib: null,
                NfeUTrib: null,
                NfeQTrib: null,
                NfeVUnTrib: null,
                NfeIcmsOrig: null,
                NfeIcmsCst: null,
                NfeIcmsModBc: null,
                NfeIcmsVBc: null,
                NfeIcmsPIcms: null,
                NfeIcmsVIcms: null,
                NfePisCst: null,
                NfePisVBc: null,
                NfePisPPis: null,
                NfePisVPis: null,
                NfeCofinsCst: null,
                NfeCofinsVBc: null,
                NfeCofinsPCofins: null,
                NfeCofinsVCofins: null,
                TracksStock: false,
                StockBaseUnit: null,
                StockOnHandBaseQty: null,
                IsActive: true,
                CreatedAt: now,
                UpdatedAt: now
            ),
            CancellationToken.None
        );

        await skus.ReplaceStockConsumptionsAsync(
            tenantId,
            porcaoId,
            new List<SkuStockConsumption>
            {
                new(
                    Id: Guid.NewGuid(),
                    TenantId: tenantId,
                    SkuId: porcaoId,
                    SourceSkuId: batataId,
                    QuantityBase: 200m
                )
            }.AsReadOnly(),
            CancellationToken.None
        );

        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var order = new Order(
            Id: orderId,
            TenantId: tenantId,
            CartId: null,
            Fulfillment: OrderFulfillment.TakeAway,
            TotalCents: 1500,
            Status: OrderStatus.Created,
            KitchenStatus: OrderKitchenStatus.PendingPayment,
            Comanda: null,
            CreatedAt: now,
            UpdatedAt: now,
            QueuedAt: null,
            InPreparationAt: null,
            ReadyAt: null,
            CompletedAt: null,
            CancelledAt: null
        );

        var items = new List<OrderItem>
        {
            new(
                Id: Guid.NewGuid(),
                TenantId: tenantId,
                OrderId: orderId,
                SkuId: porcaoId,
                SkuCode: "00011",
                SkuName: "Porção batata 200g",
                UnitPriceCents: 1500,
                Quantity: 1,
                TotalCents: 1500,
                CreatedAt: now
            )
        }.AsReadOnly();

        var payment = new Payment(
            Id: paymentId,
            TenantId: tenantId,
            OrderId: orderId,
            Method: PaymentMethod.CreditCard,
            Status: PaymentStatus.Pending,
            AmountCents: 1500,
            Provider: "TEF",
            ProviderReference: "ref",
            TransactionId: "tx",
            PixPayload: null,
            PixExpiresAt: null,
            CreatedAt: now,
            UpdatedAt: now
        );

        await checkout.CreateAsync(order, items, payment, CancellationToken.None);

        var useCase = new ConfirmPayment(checkout, tef, carts, skus, NullLogger<ConfirmPayment>.Instance);
        var result = await useCase.HandleAsync(new ConfirmPaymentCommand(tenantId, paymentId), CancellationToken.None);

        Assert.NotNull(result);
        var updatedBatata = await skus.GetByIdAsync(tenantId, batataId, CancellationToken.None);
        Assert.NotNull(updatedBatata);
        Assert.Equal(9800m, updatedBatata!.StockOnHandBaseQty);
    }

    [Fact]
    public async Task ConfirmPayment_CreatesLedgerEntry_WithOrderPaymentOrigin()
    {
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var skus = new InMemorySkuRepository();
        var carts = new InMemoryCartRepository();
        var checkout = new InMemoryCheckoutRepository();
        var tef = new ApprovedTefPaymentService();

        var batataId = Guid.NewGuid();
        var porcaoId = Guid.NewGuid();

        await skus.AddAsync(
            new Sku(
                Id: batataId, TenantId: tenantId, CategoryCode: "00001", Code: "00010",
                Name: "Batata (base)", PriceCents: 0, AveragePrepSeconds: null, ImageUrl: null,
                NfeCProd: null, NfeCEan: null, NfeCfop: null, NfeUCom: null, NfeQCom: null,
                NfeVUnCom: null, NfeVProd: null, NfeCEanTrib: null, NfeUTrib: null, NfeQTrib: null,
                NfeVUnTrib: null, NfeIcmsOrig: null, NfeIcmsCst: null, NfeIcmsModBc: null,
                NfeIcmsVBc: null, NfeIcmsPIcms: null, NfeIcmsVIcms: null, NfePisCst: null,
                NfePisVBc: null, NfePisPPis: null, NfePisVPis: null, NfeCofinsCst: null,
                NfeCofinsVBc: null, NfeCofinsPCofins: null, NfeCofinsVCofins: null,
                TracksStock: true, StockBaseUnit: StockBaseUnit.Gram, StockOnHandBaseQty: 5000m,
                IsActive: true, CreatedAt: now, UpdatedAt: now
            ),
            CancellationToken.None
        );

        await skus.AddAsync(
            new Sku(
                Id: porcaoId, TenantId: tenantId, CategoryCode: "00001", Code: "00011",
                Name: "Porção batata 200g", PriceCents: 1500, AveragePrepSeconds: null, ImageUrl: null,
                NfeCProd: null, NfeCEan: null, NfeCfop: null, NfeUCom: null, NfeQCom: null,
                NfeVUnCom: null, NfeVProd: null, NfeCEanTrib: null, NfeUTrib: null, NfeQTrib: null,
                NfeVUnTrib: null, NfeIcmsOrig: null, NfeIcmsCst: null, NfeIcmsModBc: null,
                NfeIcmsVBc: null, NfeIcmsPIcms: null, NfeIcmsVIcms: null, NfePisCst: null,
                NfePisVBc: null, NfePisPPis: null, NfePisVPis: null, NfeCofinsCst: null,
                NfeCofinsVBc: null, NfeCofinsPCofins: null, NfeCofinsVCofins: null,
                TracksStock: false, StockBaseUnit: null, StockOnHandBaseQty: null,
                IsActive: true, CreatedAt: now, UpdatedAt: now
            ),
            CancellationToken.None
        );

        await skus.ReplaceStockConsumptionsAsync(
            tenantId, porcaoId,
            new List<SkuStockConsumption>
            {
                new(Id: Guid.NewGuid(), TenantId: tenantId, SkuId: porcaoId, SourceSkuId: batataId, QuantityBase: 200m)
            }.AsReadOnly(),
            CancellationToken.None
        );

        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var order = new Order(Id: orderId, TenantId: tenantId, CartId: null,
            Fulfillment: OrderFulfillment.TakeAway, TotalCents: 1500, Status: OrderStatus.Created,
            KitchenStatus: OrderKitchenStatus.PendingPayment, Comanda: null,
            CreatedAt: now, UpdatedAt: now, QueuedAt: null, InPreparationAt: null,
            ReadyAt: null, CompletedAt: null, CancelledAt: null);

        var items = new List<OrderItem>
        {
            new(Id: Guid.NewGuid(), TenantId: tenantId, OrderId: orderId, SkuId: porcaoId,
                SkuCode: "00011", SkuName: "Porção batata 200g", UnitPriceCents: 1500, Quantity: 1,
                TotalCents: 1500, CreatedAt: now)
        }.AsReadOnly();

        var payment = new Payment(Id: paymentId, TenantId: tenantId, OrderId: orderId,
            Method: PaymentMethod.CreditCard, Status: PaymentStatus.Pending, AmountCents: 1500,
            Provider: "TEF", ProviderReference: "ref", TransactionId: "tx",
            PixPayload: null, PixExpiresAt: null, CreatedAt: now, UpdatedAt: now);

        await checkout.CreateAsync(order, items, payment, CancellationToken.None);

        var useCase = new ConfirmPayment(checkout, tef, carts, skus, NullLogger<ConfirmPayment>.Instance);
        await useCase.HandleAsync(new ConfirmPaymentCommand(tenantId, paymentId), CancellationToken.None);

        // Verifica que o ledger foi gerado com a origem correta
        var ledger = await skus.ListStockLedgerAsync(tenantId, batataId, 10, null, null, CancellationToken.None);
        Assert.Single(ledger);
        Assert.Equal(StockLedgerOriginType.OrderPayment, ledger[0].OriginType);
        Assert.Equal(orderId, ledger[0].OriginId);
        Assert.Equal(-200m, ledger[0].DeltaBaseQty);
        Assert.Equal(4800m, ledger[0].StockAfterBaseQty);
    }

    [Fact]
    public async Task AddSkuStockEntry_CreatesManualEntryLedger()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var skus = new InMemorySkuRepository();
        var batataId = Guid.NewGuid();

        await skus.AddAsync(
            new Sku(
                Id: batataId, TenantId: tenantId, CategoryCode: "00001", Code: "00010",
                Name: "Batata", PriceCents: 0, AveragePrepSeconds: null, ImageUrl: null,
                NfeCProd: null, NfeCEan: null, NfeCfop: null, NfeUCom: null, NfeQCom: null,
                NfeVUnCom: null, NfeVProd: null, NfeCEanTrib: null, NfeUTrib: null, NfeQTrib: null,
                NfeVUnTrib: null, NfeIcmsOrig: null, NfeIcmsCst: null, NfeIcmsModBc: null,
                NfeIcmsVBc: null, NfeIcmsPIcms: null, NfeIcmsVIcms: null, NfePisCst: null,
                NfePisVBc: null, NfePisPPis: null, NfePisVPis: null, NfeCofinsCst: null,
                NfeCofinsVBc: null, NfeCofinsPCofins: null, NfeCofinsVCofins: null,
                TracksStock: true, StockBaseUnit: StockBaseUnit.Gram, StockOnHandBaseQty: 0m,
                IsActive: true, CreatedAt: now, UpdatedAt: now
            ),
            CancellationToken.None
        );

        var useCase = new AddSkuStockEntry(skus, NullLogger<AddSkuStockEntry>.Instance);
        await useCase.HandleAsync(
            new AddSkuStockEntryCommand(tenantId, batataId, 2, "kg", userId, "Reposição de estoque"),
            CancellationToken.None
        );

        var updated = await skus.GetByIdAsync(tenantId, batataId, CancellationToken.None);
        Assert.Equal(2000m, updated!.StockOnHandBaseQty);

        var ledger = await skus.ListStockLedgerAsync(tenantId, batataId, 10, null, null, CancellationToken.None);
        Assert.Single(ledger);
        Assert.Equal(StockLedgerOriginType.ManualEntry, ledger[0].OriginType);
        Assert.Equal(2000m, ledger[0].DeltaBaseQty);
        Assert.Equal(2000m, ledger[0].StockAfterBaseQty);
        Assert.Equal(userId, ledger[0].ActorUserId);
        Assert.Equal("Reposição de estoque", ledger[0].Notes);
    }

    private sealed class ApprovedTefPaymentService : ITefPaymentService
    {
        public Task<TefPixCharge> CreatePixChargeAsync(int amountCents, string reference, CancellationToken ct)
        {
            return Task.FromResult(new TefPixCharge("payload", DateTimeOffset.UtcNow.AddMinutes(5), "ref"));
        }

        public Task<string> StartCardAsync(int amountCents, PaymentMethod method, string reference, CancellationToken ct)
        {
            return Task.FromResult("ref");
        }

        public Task<TefPaymentConfirmation> ConfirmAsync(string providerReference, PaymentMethod method, CancellationToken ct)
        {
            return Task.FromResult(new TefPaymentConfirmation(true, "APPROVED", null));
        }
    }
}
