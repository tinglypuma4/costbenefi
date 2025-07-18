using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace costbenefi.Migrations
{
    public partial class ActualizarMovimientos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cliente",
                table: "Movimientos",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumeroDocumento",
                table: "Movimientos",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Proveedor",
                table: "Movimientos",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "StockAnterior",
                table: "Movimientos",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StockPosterior",
                table: "Movimientos",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cliente",
                table: "Movimientos");

            migrationBuilder.DropColumn(
                name: "NumeroDocumento",
                table: "Movimientos");

            migrationBuilder.DropColumn(
                name: "Proveedor",
                table: "Movimientos");

            migrationBuilder.DropColumn(
                name: "StockAnterior",
                table: "Movimientos");

            migrationBuilder.DropColumn(
                name: "StockPosterior",
                table: "Movimientos");
        }
    }
}
