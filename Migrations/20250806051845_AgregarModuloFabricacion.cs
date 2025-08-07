using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace costbenefi.Migrations
{
    public partial class AgregarModuloFabricacion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDescuentosAplicados",
                table: "Ventas",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<bool>(
                name: "TieneDescuentosAplicados",
                table: "Ventas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<bool>(
                name: "TieneDescuentoManual",
                table: "DetalleVentas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioOriginal",
                table: "DetalleVentas",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "DescuentoUnitario",
                table: "DetalleVentas",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.CreateTable(
                name: "ProcesosFabricacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombreProducto = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CategoriaProducto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    RendimientoEsperado = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    UnidadMedidaProducto = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "L"),
                    TiempoFabricacionMinutos = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 60),
                    PorcentajeMerma = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    CostoManoObra = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    IncluirCostoEnergia = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CostoEnergia = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    IncluirCostoTransporte = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CostoTransporte = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    IncluirCostoEmpaque = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CostoEmpaque = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    IncluirOtrosCostos = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    OtrosCostos = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    DescripcionOtrosCostos = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TipoFabricacion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "Lote"),
                    MargenObjetivo = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 30m),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    NotasEspeciales = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    FechaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UsuarioCreador = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcesosFabricacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LotesFabricacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProcesoFabricacionId = table.Column<int>(type: "INTEGER", nullable: false),
                    NumeroLote = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CantidadPlanificada = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    CantidadObtenida = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    FechaFin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Planificado"),
                    CostoMaterialesReal = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    CostoManoObraReal = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    CostosAdicionalesReal = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    OperadorResponsable = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    NotasProduccion = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    ProductoResultanteId = table.Column<int>(type: "INTEGER", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotesFabricacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LotesFabricacion_ProcesosFabricacion_ProcesoFabricacionId",
                        column: x => x.ProcesoFabricacionId,
                        principalTable: "ProcesosFabricacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LotesFabricacion_RawMaterials_ProductoResultanteId",
                        column: x => x.ProductoResultanteId,
                        principalTable: "RawMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RecetaDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProcesoFabricacionId = table.Column<int>(type: "INTEGER", nullable: false),
                    RawMaterialId = table.Column<int>(type: "INTEGER", nullable: false),
                    CantidadRequerida = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    UnidadMedida = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 0m),
                    EsIngredientePrincipal = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    OrdenAdicion = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    NotasIngrediente = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    FechaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecetaDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecetaDetalles_ProcesosFabricacion_ProcesoFabricacionId",
                        column: x => x.ProcesoFabricacionId,
                        principalTable: "ProcesosFabricacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecetaDetalles_RawMaterials_RawMaterialId",
                        column: x => x.RawMaterialId,
                        principalTable: "RawMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_FechaHoraDescuento",
                table: "Ventas",
                column: "FechaHoraDescuento");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_TieneDescuentosAplicados",
                table: "Ventas",
                column: "TieneDescuentosAplicados");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_TipoUsuarioAutorizador",
                table: "Ventas",
                column: "TipoUsuarioAutorizador");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_UsuarioAutorizadorDescuento",
                table: "Ventas",
                column: "UsuarioAutorizadorDescuento");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleVentas_PrecioOriginal",
                table: "DetalleVentas",
                column: "PrecioOriginal");

            migrationBuilder.CreateIndex(
                name: "IX_DetalleVentas_TieneDescuentoManual",
                table: "DetalleVentas",
                column: "TieneDescuentoManual");

            migrationBuilder.CreateIndex(
                name: "IX_LotesFabricacion_Estado",
                table: "LotesFabricacion",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_LotesFabricacion_FechaFin",
                table: "LotesFabricacion",
                column: "FechaFin");

            migrationBuilder.CreateIndex(
                name: "IX_LotesFabricacion_FechaInicio",
                table: "LotesFabricacion",
                column: "FechaInicio");

            migrationBuilder.CreateIndex(
                name: "IX_LotesFabricacion_NumeroLote",
                table: "LotesFabricacion",
                column: "NumeroLote",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LotesFabricacion_OperadorResponsable",
                table: "LotesFabricacion",
                column: "OperadorResponsable");

            migrationBuilder.CreateIndex(
                name: "IX_LotesFabricacion_ProcesoFabricacionId",
                table: "LotesFabricacion",
                column: "ProcesoFabricacionId");

            migrationBuilder.CreateIndex(
                name: "IX_LotesFabricacion_ProductoResultanteId",
                table: "LotesFabricacion",
                column: "ProductoResultanteId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcesosFabricacion_Activo",
                table: "ProcesosFabricacion",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_ProcesosFabricacion_CategoriaProducto",
                table: "ProcesosFabricacion",
                column: "CategoriaProducto");

            migrationBuilder.CreateIndex(
                name: "IX_ProcesosFabricacion_FechaCreacion",
                table: "ProcesosFabricacion",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_ProcesosFabricacion_NombreProducto",
                table: "ProcesosFabricacion",
                column: "NombreProducto");

            migrationBuilder.CreateIndex(
                name: "IX_ProcesosFabricacion_TipoFabricacion",
                table: "ProcesosFabricacion",
                column: "TipoFabricacion");

            migrationBuilder.CreateIndex(
                name: "IX_ProcesosFabricacion_UsuarioCreador",
                table: "ProcesosFabricacion",
                column: "UsuarioCreador");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaDetalles_EsIngredientePrincipal",
                table: "RecetaDetalles",
                column: "EsIngredientePrincipal");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaDetalles_OrdenAdicion",
                table: "RecetaDetalles",
                column: "OrdenAdicion");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaDetalles_ProcesoFabricacionId",
                table: "RecetaDetalles",
                column: "ProcesoFabricacionId");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaDetalles_RawMaterialId",
                table: "RecetaDetalles",
                column: "RawMaterialId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LotesFabricacion");

            migrationBuilder.DropTable(
                name: "RecetaDetalles");

            migrationBuilder.DropTable(
                name: "ProcesosFabricacion");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_FechaHoraDescuento",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_TieneDescuentosAplicados",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_TipoUsuarioAutorizador",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_UsuarioAutorizadorDescuento",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_DetalleVentas_PrecioOriginal",
                table: "DetalleVentas");

            migrationBuilder.DropIndex(
                name: "IX_DetalleVentas_TieneDescuentoManual",
                table: "DetalleVentas");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalDescuentosAplicados",
                table: "Ventas",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<bool>(
                name: "TieneDescuentosAplicados",
                table: "Ventas",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "TieneDescuentoManual",
                table: "DetalleVentas",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioOriginal",
                table: "DetalleVentas",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "DescuentoUnitario",
                table: "DetalleVentas",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldDefaultValue: 0m);
        }
    }
}
