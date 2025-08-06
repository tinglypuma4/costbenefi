using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace costbenefi.Migrations
{
    public partial class AgregarAuditoriaDescuentos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaHoraDescuento",
                table: "Ventas",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoDescuentoGeneral",
                table: "Ventas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "TieneDescuentosAplicados",
                table: "Ventas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TipoUsuarioAutorizador",
                table: "Ventas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalDescuentosAplicados",
                table: "Ventas",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioAutorizadorDescuento",
                table: "Ventas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoUnitario",
                table: "DetalleVentas",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "MotivoDescuentoDetalle",
                table: "DetalleVentas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioOriginal",
                table: "DetalleVentas",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "TieneDescuentoManual",
                table: "DetalleVentas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaHoraDescuento",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MotivoDescuentoGeneral",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "TieneDescuentosAplicados",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "TipoUsuarioAutorizador",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "TotalDescuentosAplicados",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "UsuarioAutorizadorDescuento",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "DescuentoUnitario",
                table: "DetalleVentas");

            migrationBuilder.DropColumn(
                name: "MotivoDescuentoDetalle",
                table: "DetalleVentas");

            migrationBuilder.DropColumn(
                name: "PrecioOriginal",
                table: "DetalleVentas");

            migrationBuilder.DropColumn(
                name: "TieneDescuentoManual",
                table: "DetalleVentas");
        }
    }
}
