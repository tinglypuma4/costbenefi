using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace costbenefi.Migrations
{
    public partial class AddBarcodeAndIVAFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RawMaterials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombreArticulo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Categoria = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UnidadMedida = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StockAntiguo = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    StockNuevo = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PrecioPorUnidad = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PrecioPorUnidadBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Proveedor = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AlertaStockBajo = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CodigoBarras = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PrecioConIVA = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PrecioSinIVA = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawMaterials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_Categoria",
                table: "RawMaterials",
                column: "Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_NombreArticulo",
                table: "RawMaterials",
                column: "NombreArticulo");

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_Proveedor",
                table: "RawMaterials",
                column: "Proveedor");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RawMaterials");
        }
    }
}
