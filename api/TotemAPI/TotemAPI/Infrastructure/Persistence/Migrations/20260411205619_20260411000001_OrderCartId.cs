using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260411000001_OrderCartId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CartId",
                table: "orders",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CartId",
                table: "orders");
        }
    }
}
