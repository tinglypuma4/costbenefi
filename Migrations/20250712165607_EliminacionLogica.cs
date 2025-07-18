using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace costbenefi.Migrations
{
    public partial class EliminacionLogica : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movimientos_RawMaterials_RawMaterialId",
                table: "Movimientos");

            migrationBuilder.AddColumn<bool>(
                name: "Eliminado",
                table: "RawMaterials",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEliminacion",
                table: "RawMaterials",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoEliminacion",
                table: "RawMaterials",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioEliminacion",
                table: "RawMaterials",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_Eliminado",
                table: "RawMaterials",
                column: "Eliminado");

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_FechaEliminacion",
                table: "RawMaterials",
                column: "FechaEliminacion");

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_UsuarioEliminacion",
                table: "RawMaterials",
                column: "UsuarioEliminacion");

            migrationBuilder.AddForeignKey(
                name: "FK_Movimientos_RawMaterials_RawMaterialId",
                table: "Movimientos",
                column: "RawMaterialId",
                principalTable: "RawMaterials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movimientos_RawMaterials_RawMaterialId",
                table: "Movimientos");

            migrationBuilder.DropIndex(
                name: "IX_RawMaterials_Eliminado",
                table: "RawMaterials");

            migrationBuilder.DropIndex(
                name: "IX_RawMaterials_FechaEliminacion",
                table: "RawMaterials");

            migrationBuilder.DropIndex(
                name: "IX_RawMaterials_UsuarioEliminacion",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "Eliminado",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "FechaEliminacion",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "MotivoEliminacion",
                table: "RawMaterials");

            migrationBuilder.DropColumn(
                name: "UsuarioEliminacion",
                table: "RawMaterials");

            migrationBuilder.AddForeignKey(
                name: "FK_Movimientos_RawMaterials_RawMaterialId",
                table: "Movimientos",
                column: "RawMaterialId",
                principalTable: "RawMaterials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
