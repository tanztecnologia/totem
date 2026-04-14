using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TotemAPI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SkuNfeProdFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NfeCEan",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NfeCEanTrib",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NfeCProd",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NfeCfop",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NfeQCom",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NfeQTrib",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NfeUCom",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NfeUTrib",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NfeVProd",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NfeVUnCom",
                table: "skus",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NfeVUnTrib",
                table: "skus",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NfeCEan",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeCEanTrib",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeCProd",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeCfop",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeQCom",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeQTrib",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeUCom",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeUTrib",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeVProd",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeVUnCom",
                table: "skus");

            migrationBuilder.DropColumn(
                name: "NfeVUnTrib",
                table: "skus");
        }
    }
}
