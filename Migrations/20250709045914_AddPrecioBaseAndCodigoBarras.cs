using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace costbenefi.Migrations
{
    public partial class AddPrecioBaseAndCodigoBarras : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PrecioBaseConIVA",
                table: "RawMaterials",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioBaseSinIVA",
                table: "RawMaterials",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_CodigoBarras",
                table: "RawMaterials",
                column: "CodigoBarras");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RawMaterials_CodigoBarras",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "PrecioBaseConIVA",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "PrecioBaseSinIVA",
                table: "RawMaterials");
        }
    }
}
