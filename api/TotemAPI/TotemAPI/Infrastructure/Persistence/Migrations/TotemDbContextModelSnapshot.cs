using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace TotemAPI.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TotemDbContext))]
public sealed partial class TotemDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "9.0.11");

        modelBuilder.Entity("TotemAPI.Infrastructure.Persistence.TenantRow", b =>
        {
            b.Property<Guid>("Id").HasColumnType("TEXT");
            b.Property<DateTimeOffset>("CreatedAt").HasColumnType("TEXT");
            b.Property<string>("Name").IsRequired().HasColumnType("TEXT");
            b.Property<string>("NormalizedName").IsRequired().HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("NormalizedName").IsUnique();
            b.ToTable("tenants");
        });

        modelBuilder.Entity("TotemAPI.Infrastructure.Persistence.UserRow", b =>
        {
            b.Property<Guid>("Id").HasColumnType("TEXT");
            b.Property<DateTimeOffset>("CreatedAt").HasColumnType("TEXT");
            b.Property<string>("Email").IsRequired().HasColumnType("TEXT");
            b.Property<string>("NormalizedEmail").IsRequired().HasColumnType("TEXT");
            b.Property<string>("PasswordHash").IsRequired().HasColumnType("TEXT");
            b.Property<int>("Role").HasColumnType("INTEGER");
            b.Property<Guid>("TenantId").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("NormalizedEmail").IsUnique();
            b.HasIndex("TenantId", "NormalizedEmail").IsUnique();
            b.ToTable("users");
        });

        modelBuilder.Entity("TotemAPI.Infrastructure.Persistence.SkuRow", b =>
        {
            b.Property<Guid>("Id").HasColumnType("TEXT");
            b.Property<string>("Code").IsRequired().HasColumnType("TEXT");
            b.Property<DateTimeOffset>("CreatedAt").HasColumnType("TEXT");
            b.Property<string?>("ImageUrl").HasColumnType("TEXT");
            b.Property<bool>("IsActive").HasColumnType("INTEGER");
            b.Property<string>("Name").IsRequired().HasColumnType("TEXT");
            b.Property<string>("NormalizedCode").IsRequired().HasColumnType("TEXT");
            b.Property<int>("PriceCents").HasColumnType("INTEGER");
            b.Property<Guid>("TenantId").HasColumnType("TEXT");
            b.Property<DateTimeOffset>("UpdatedAt").HasColumnType("TEXT");
            b.HasKey("Id");
            b.HasIndex("TenantId");
            b.HasIndex("TenantId", "NormalizedCode").IsUnique();
            b.ToTable("skus");
        });
    }
}
