using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace costbenefi.Migrations
{
    public partial class AgregarSoporteServiciosEnVentas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RawMaterialId",
                table: "DetalleVentas",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "ServicioVentaId",
                table: "DetalleVentas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetalleVentas_ServicioVentaId",
                table: "DetalleVentas",
                column: "ServicioVentaId");

            migrationBuilder.AddForeignKey(
                name: "FK_DetalleVentas_ServiciosVenta_ServicioVentaId",
                table: "DetalleVentas",
                column: "ServicioVentaId",
                principalTable: "ServiciosVenta",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetalleVentas_ServiciosVenta_ServicioVentaId",
                table: "DetalleVentas");

            migrationBuilder.DropIndex(
                name: "IX_DetalleVentas_ServicioVentaId",
                table: "DetalleVentas");

            migrationBuilder.DropColumn(
                name: "ServicioVentaId",
                table: "DetalleVentas");

            migrationBuilder.AlterColumn<int>(
                name: "RawMaterialId",
                table: "DetalleVentas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
