using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace costbenefi.Migrations
{
    public partial class AgregarModuloServicios : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Rol",
                table: "Users",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Cajero",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20,
                oldDefaultValue: "Vendedor");

            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEliminacion",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoEliminacion",
                table: "Users",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioEliminacion",
                table: "Users",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PromocionesVenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombrePromocion = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TipoPromocion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "DescuentoPorcentaje"),
                    CategoriaPromocion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "General"),
                    ValorPromocion = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    DescuentoMaximo = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    MontoMinimo = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    CantidadMinima = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now', '+30 days')"),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    AplicacionAutomatica = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IntegradaPOS = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Prioridad = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 100),
                    LimitePorCliente = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    LimiteUsoTotal = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    VecesUsada = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CodigoPromocion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductosAplicables = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ServiciosAplicables = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CategoriasAplicables = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DiasAplicables = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "L,M,Mi,J,V,S,D"),
                    HoraInicio = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false, defaultValue: "00:00"),
                    HoraFin = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false, defaultValue: "23:59"),
                    RequiereCodigo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Combinable = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    FechaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UsuarioCreador = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Eliminado = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    FechaEliminacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioEliminacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MotivoEliminacion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromocionesVenta", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiciosVenta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombreServicio = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CategoriaServicio = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PrecioBase = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    PrecioServicio = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    DuracionEstimada = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "30 min"),
                    CostoMateriales = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    CostoManoObra = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    MargenObjetivo = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 40m),
                    PorcentajeIVA = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 16m),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IntegradoPOS = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    PrioridadPOS = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 100),
                    RequiereConfirmacion = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    LimiteDiario = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    StockDisponible = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 999m),
                    CodigoServicio = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    FechaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UsuarioCreador = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Eliminado = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    FechaEliminacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsuarioEliminacion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MotivoEliminacion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiciosVenta", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaterialesServicio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServicioVentaId = table.Column<int>(type: "INTEGER", nullable: false),
                    RawMaterialId = table.Column<int>(type: "INTEGER", nullable: false),
                    CantidadNecesaria = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    UnidadMedida = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PorcentajeDesperdicio = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    EsOpcional = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    OrdenUso = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    TiempoUsoMinutos = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    VerificarDisponibilidad = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    StockMinimoRequerido = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    FechaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UsuarioCreador = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialesServicio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialesServicio_RawMaterials_RawMaterialId",
                        column: x => x.RawMaterialId,
                        principalTable: "RawMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialesServicio_ServiciosVenta_ServicioVentaId",
                        column: x => x.ServicioVentaId,
                        principalTable: "ServiciosVenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialesServicio_EsOpcional",
                table: "MaterialesServicio",
                column: "EsOpcional");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialesServicio_OrdenUso",
                table: "MaterialesServicio",
                column: "OrdenUso");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialesServicio_RawMaterialId",
                table: "MaterialesServicio",
                column: "RawMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialesServicio_ServicioVentaId",
                table: "MaterialesServicio",
                column: "ServicioVentaId");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionesVenta_Activa",
                table: "PromocionesVenta",
                column: "Activa");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionesVenta_CategoriaPromocion",
                table: "PromocionesVenta",
                column: "CategoriaPromocion");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionesVenta_CodigoPromocion",
                table: "PromocionesVenta",
                column: "CodigoPromocion");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionesVenta_Eliminado",
                table: "PromocionesVenta",
                column: "Eliminado");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionesVenta_FechaFin",
                table: "PromocionesVenta",
                column: "FechaFin");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionesVenta_FechaInicio",
                table: "PromocionesVenta",
                column: "FechaInicio");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionesVenta_IntegradaPOS",
                table: "PromocionesVenta",
                column: "IntegradaPOS");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionesVenta_NombrePromocion",
                table: "PromocionesVenta",
                column: "NombrePromocion");

            migrationBuilder.CreateIndex(
                name: "IX_PromocionesVenta_TipoPromocion",
                table: "PromocionesVenta",
                column: "TipoPromocion");

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosVenta_Activo",
                table: "ServiciosVenta",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosVenta_CategoriaServicio",
                table: "ServiciosVenta",
                column: "CategoriaServicio");

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosVenta_CodigoServicio",
                table: "ServiciosVenta",
                column: "CodigoServicio");

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosVenta_Eliminado",
                table: "ServiciosVenta",
                column: "Eliminado");

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosVenta_FechaCreacion",
                table: "ServiciosVenta",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosVenta_IntegradoPOS",
                table: "ServiciosVenta",
                column: "IntegradoPOS");

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosVenta_NombreServicio",
                table: "ServiciosVenta",
                column: "NombreServicio");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialesServicio");

            migrationBuilder.DropTable(
                name: "PromocionesVenta");

            migrationBuilder.DropTable(
                name: "ServiciosVenta");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FechaEliminacion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MotivoEliminacion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UsuarioEliminacion",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Rol",
                table: "Users",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Vendedor",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20,
                oldDefaultValue: "Cajero");
        }
    }
}
