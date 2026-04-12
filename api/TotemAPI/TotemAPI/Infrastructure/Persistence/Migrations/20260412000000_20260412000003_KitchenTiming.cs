using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations;

[DbContext(typeof(TotemDbContext))]
[Migration("20260412000000_20260412000003_KitchenTiming")]
public sealed partial class _20260412000003_KitchenTiming : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "QueuedAt",
            table: "orders",
            type: "TEXT",
            nullable: true
        );

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "InPreparationAt",
            table: "orders",
            type: "TEXT",
            nullable: true
        );

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ReadyAt",
            table: "orders",
            type: "TEXT",
            nullable: true
        );

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CompletedAt",
            table: "orders",
            type: "TEXT",
            nullable: true
        );

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CancelledAt",
            table: "orders",
            type: "TEXT",
            nullable: true
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "QueuedAt", table: "orders");
        migrationBuilder.DropColumn(name: "InPreparationAt", table: "orders");
        migrationBuilder.DropColumn(name: "ReadyAt", table: "orders");
        migrationBuilder.DropColumn(name: "CompletedAt", table: "orders");
        migrationBuilder.DropColumn(name: "CancelledAt", table: "orders");
    }
}
