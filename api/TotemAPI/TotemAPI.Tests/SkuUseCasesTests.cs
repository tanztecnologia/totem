using TotemAPI.Features.Catalog.Application.UseCases;
using TotemAPI.Features.Catalog.Infrastructure;
using Xunit;

namespace TotemAPI.Tests;

public sealed class SkuUseCasesTests
{
    [Fact]
    public async Task Crud_respeita_segregacao_por_tenant()
    {
        var repo = new InMemorySkuRepository();
        var create = new CreateSku(repo);
        var get = new GetSku(repo);
        var list = new ListSkus(repo);
        var update = new UpdateSku(repo);
        var delete = new DeleteSku(repo);

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var created = await create.HandleAsync(
            new CreateSkuCommand(tenantA, "X-BURGER", "X Burger", 2500, null, null, true),
            CancellationToken.None
        );

        var sameTenantGet = await get.HandleAsync(new GetSkuQuery(tenantA, created.Id), CancellationToken.None);
        Assert.NotNull(sameTenantGet);

        var otherTenantGet = await get.HandleAsync(new GetSkuQuery(tenantB, created.Id), CancellationToken.None);
        Assert.Null(otherTenantGet);

        var updatedOtherTenant = await update.HandleAsync(
            new UpdateSkuCommand(tenantB, created.Id, "X-BURGER", "Novo Nome", 2600, null, null, true),
            CancellationToken.None
        );
        Assert.Null(updatedOtherTenant);

        var deletedOtherTenant = await delete.HandleAsync(new DeleteSkuCommand(tenantB, created.Id), CancellationToken.None);
        Assert.False(deletedOtherTenant);

        var listA = await list.HandleAsync(new ListSkusQuery(tenantA), CancellationToken.None);
        Assert.Single(listA);

        var listB = await list.HandleAsync(new ListSkusQuery(tenantB), CancellationToken.None);
        Assert.Empty(listB);
    }

    [Fact]
    public async Task Nao_permite_code_duplicado_no_mesmo_tenant()
    {
        var repo = new InMemorySkuRepository();
        var create = new CreateSku(repo);

        var tenantA = Guid.NewGuid();

        await create.HandleAsync(
            new CreateSkuCommand(tenantA, "X-BURGER", "X Burger", 2500, null, null, true),
            CancellationToken.None
        );

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () =>
                await create.HandleAsync(
                    new CreateSkuCommand(tenantA, "X-BURGER", "X Burger 2", 2600, null, null, true),
                    CancellationToken.None
                )
        );
    }

    [Fact]
    public async Task Permite_mesmo_code_em_tenants_diferentes()
    {
        var repo = new InMemorySkuRepository();
        var create = new CreateSku(repo);

        await create.HandleAsync(
            new CreateSkuCommand(Guid.NewGuid(), "X-BURGER", "X Burger", 2500, null, null, true),
            CancellationToken.None
        );

        var created = await create.HandleAsync(
            new CreateSkuCommand(Guid.NewGuid(), "X-BURGER", "X Burger", 2500, null, null, true),
            CancellationToken.None
        );

        Assert.NotEqual(Guid.Empty, created.Id);
    }
}
