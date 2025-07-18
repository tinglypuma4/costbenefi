using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace costbenefi.Migrations
{
    public partial class AgregarSistemaPOS : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ActivoParaVenta",
                table: "RawMaterials",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "DiasParaDescuento",
                table: "RawMaterials",
                type: "INTEGER",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaVencimiento",
                table: "RawMaterials",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MargenObjetivo",
                table: "RawMaterials",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 30m);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeDescuento",
                table: "RawMaterials",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioDescuento",
                table: "RawMaterials",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioVenta",
                table: "RawMaterials",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioVentaConIVA",
                table: "RawMaterials",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StockMinimoVenta",
                table: "RawMaterials",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.CreateTable(
                name: "Ventas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FechaVenta = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    Cliente = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Usuario = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IVA = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    FormaPago = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    NumeroTicket = table.Column<int>(type: "INTEGER", nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Descuento = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ventas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DetalleVentas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VentaId = table.Column<int>(type: "INTEGER", nullable: false),
                    RawMaterialId = table.Column<int>(type: "INTEGER", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    NombreProducto = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    UnidadMedida = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PorcentajeIVA = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 16.0m),
                    DescuentoAplicado = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetalleVentas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetalleVentas_RawMaterials_RawMaterialId",
                        column: x => x.RawMaterialId,
                        principalTable: "RawMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetalleVentas_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_ActivoParaVenta",
                table: "RawMaterials",
                column: "ActivoParaVenta");

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_FechaVencimiento",
                table: "RawMaterials",
                column: "FechaVencimiento");

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_PrecioVenta",
                table: "RawMaterials",
                column: "PrecioVenta");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleVentas_RawMaterialId",
                table: "DetalleVentas",
                column: "RawMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleVentas_VentaId",
                table: "DetalleVentas",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_Estado",
                table: "Ventas",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_FechaVenta",
                table: "Ventas",
                column: "FechaVenta");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_NumeroTicket",
                table: "Ventas",
                column: "NumeroTicket",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_Usuario",
                table: "Ventas",
                column: "Usuario");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetalleVentas");

            migrationBuilder.DropTable(
                name: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_RawMaterials_ActivoParaVenta",
                table: "RawMaterials");

            migrationBuilder.DropIndex(
                name: "IX_RawMaterials_FechaVencimiento",
                table: "RawMaterials");

            migrationBuilder.DropIndex(
                name: "IX_RawMaterials_PrecioVenta",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "ActivoParaVenta",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "DiasParaDescuento",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "FechaVencimiento",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "MargenObjetivo",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "PorcentajeDescuento",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "PrecioDescuento",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "PrecioVenta",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "PrecioVentaConIVA",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "StockMinimoVenta",
                table: "RawMaterials");
        }
    }
}
