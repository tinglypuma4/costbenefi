using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace costbenefi.Migrations
{
    public partial class AgregarCorteCaja : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ComisionTarjeta",
                table: "Ventas",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ComisionTotal",
                table: "Ventas",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CorteCajaId",
                table: "Ventas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IVAComision",
                table: "Ventas",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoEfectivo",
                table: "Ventas",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoTarjeta",
                table: "Ventas",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoTransferencia",
                table: "Ventas",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeComisionTarjeta",
                table: "Ventas",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ConfiguracionesBascula",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Puerto = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    BaudRate = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 9600),
                    DataBits = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 8),
                    Paridad = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    StopBits = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    ControlFlujo = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    TimeoutLectura = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1000),
                    TimeoutEscritura = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1000),
                    IntervaloLectura = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1000),
                    UnidadPeso = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false, defaultValue: "kg"),
                    TerminadorComando = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false, defaultValue: "\r\n"),
                    RequiereSolicitudPeso = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ComandoSolicitarPeso = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "P"),
                    ComandoTara = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "T"),
                    ComandoInicializacion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: ""),
                    PatronExtraccion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, defaultValue: ""),
                    EsConfiguracionActiva = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    FechaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesBascula", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CortesCaja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FechaCorte = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "date('now')"),
                    FechaHoraCorte = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UsuarioCorte = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    TotalVentasCalculado = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    CantidadTickets = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    EfectivoCalculado = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    TarjetaCalculado = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    TransferenciaCalculado = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    ComisionesCalculadas = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    IVAComisionesCalculado = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    ComisionesTotalesCalculadas = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    GananciaBrutaCalculada = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    GananciaNetaCalculada = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    EfectivoContado = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    FondoCajaInicial = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 1000m),
                    FondoCajaSiguiente = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 1000m),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false, defaultValue: ""),
                    MotivoSobrante = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false, defaultValue: ""),
                    MotivoFaltante = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false, defaultValue: ""),
                    DepositoRealizado = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    ReferenciaDeposito = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: ""),
                    MontoDepositado = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    FechaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CortesCaja", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_CorteCajaId",
                table: "Ventas",
                column: "CorteCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesBascula_EsConfiguracionActiva",
                table: "ConfiguracionesBascula",
                column: "EsConfiguracionActiva");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesBascula_Nombre",
                table: "ConfiguracionesBascula",
                column: "Nombre");

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesBascula_Puerto",
                table: "ConfiguracionesBascula",
                column: "Puerto");

            migrationBuilder.CreateIndex(
                name: "IX_CortesCaja_Estado",
                table: "CortesCaja",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_CortesCaja_FechaCorte",
                table: "CortesCaja",
                column: "FechaCorte",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CortesCaja_FechaHoraCorte",
                table: "CortesCaja",
                column: "FechaHoraCorte");

            migrationBuilder.CreateIndex(
                name: "IX_CortesCaja_UsuarioCorte",
                table: "CortesCaja",
                column: "UsuarioCorte");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_CortesCaja_CorteCajaId",
                table: "Ventas",
                column: "CorteCajaId",
                principalTable: "CortesCaja",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_CortesCaja_CorteCajaId",
                table: "Ventas");

            migrationBuilder.DropTable(
                name: "ConfiguracionesBascula");

            migrationBuilder.DropTable(
                name: "CortesCaja");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_CorteCajaId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ComisionTarjeta",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "ComisionTotal",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "CorteCajaId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "IVAComision",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MontoEfectivo",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MontoTarjeta",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MontoTransferencia",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "PorcentajeComisionTarjeta",
                table: "Ventas");
        }
    }
}
