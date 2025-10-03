using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace costbenefi.Migrations
{
    public partial class AgregarConfiguracionComisiones : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionesComisiones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PorcentajeComisionTarjeta = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 3.5m),
                    TerminalCobraIVA = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    PorcentajeIVA = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 16.0m),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false, defaultValue: "Configuración por defecto"),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    FechaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UsuarioModificacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesComisiones", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesComisiones_Activa",
                table: "ConfiguracionesComisiones",
                column: "Activa");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesComisiones_FechaCreacion",
                table: "ConfiguracionesComisiones",
                column: "FechaCreacion");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionesComisiones");
        }
    }
}
