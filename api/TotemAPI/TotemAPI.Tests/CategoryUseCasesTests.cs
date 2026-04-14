using System;
using System.Threading;
using System.Threading.Tasks;
using TotemAPI.Features.Catalog.Application.UseCases;
using TotemAPI.Features.Catalog.Infrastructure;
using Xunit;

namespace TotemAPI.Tests;

public sealed class CategoryUseCasesTests
{
    [Fact]
    public async Task CreateCategory_e_GetCategoryByCode_funcionam()
    {
        var repo = new InMemoryCategoryRepository();
        var create = new CreateCategory(repo);
        var get = new GetCategoryByCode(repo);

        var tenantId = Guid.NewGuid();

        var created = await create.HandleAsync(new CreateCategoryCommand(tenantId, "Bebidas", "drinks", true), CancellationToken.None);
        Assert.Equal("00001", created.Code);
        Assert.Equal("drinks", created.Slug);

        var found = await get.HandleAsync(new GetCategoryByCodeQuery(tenantId, "1"), CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal(created.Id, found!.Id);
        Assert.Equal("Bebidas", found.Name);
    }
}
