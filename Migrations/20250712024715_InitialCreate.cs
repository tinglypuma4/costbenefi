using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace costbenefi.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FactorConversion",
                table: "RawMaterials",
                type: "decimal(18,6)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<string>(
                name: "UnidadBase",
                table: "RawMaterials",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Movimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RawMaterialId = table.Column<int>(type: "INTEGER", nullable: false),
                    TipoMovimiento = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Motivo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Usuario = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FechaMovimiento = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PrecioConIVA = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PrecioSinIVA = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UnidadMedida = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movimientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Movimientos_RawMaterials_RawMaterialId",
                        column: x => x.RawMaterialId,
                        principalTable: "RawMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_UnidadMedida",
                table: "RawMaterials",
                column: "UnidadMedida");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_FechaMovimiento",
                table: "Movimientos",
                column: "FechaMovimiento");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_RawMaterialId",
                table: "Movimientos",
                column: "RawMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_TipoMovimiento",
                table: "Movimientos",
                column: "TipoMovimiento");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Movimientos");

            migrationBuilder.DropIndex(
                name: "IX_RawMaterials_UnidadMedida",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "FactorConversion",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "UnidadBase",
                table: "RawMaterials");
        }
    }
}
