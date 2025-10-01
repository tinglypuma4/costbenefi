using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace costbenefi.Migrations
{
    public partial class ConfiguracionBasculaActualizada : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComandoInicializacion",
                table: "ConfiguracionesBascula");

            migrationBuilder.DropColumn(
                name: "TerminadorComando",
                table: "ConfiguracionesBascula");

            migrationBuilder.DropColumn(
                name: "TimeoutEscritura",
                table: "ConfiguracionesBascula");

            migrationBuilder.RenameColumn(
                name: "Paridad",
                table: "ConfiguracionesBascula",
                newName: "Parity");

            migrationBuilder.RenameColumn(
                name: "ControlFlujo",
                table: "ConfiguracionesBascula",
                newName: "Handshake");

            migrationBuilder.AlterColumn<int>(
                name: "TimeoutLectura",
                table: "ConfiguracionesBascula",
                type: "INTEGER",
                nullable: false,
                defaultValue: 2000,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 1000);

            migrationBuilder.AlterColumn<bool>(
                name: "RequiereSolicitudPeso",
                table: "ConfiguracionesBascula",
                type: "INTEGER",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "PatronExtraccion",
                table: "ConfiguracionesBascula",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "(\\d+\\.?\\d*)",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 200,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaActualizacion",
                table: "ConfiguracionesBascula",
                type: "TEXT",
                nullable: true,
                defaultValueSql: "datetime('now')",
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldDefaultValueSql: "datetime('now')");

            migrationBuilder.AddColumn<string>(
                name: "TerminadorLinea",
                table: "ConfiguracionesBascula",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "\r\n");

            migrationBuilder.AddColumn<string>(
                name: "UsuarioCreacion",
                table: "ConfiguracionesBascula",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TerminadorLinea",
                table: "ConfiguracionesBascula");

            migrationBuilder.DropColumn(
                name: "UsuarioCreacion",
                table: "ConfiguracionesBascula");

            migrationBuilder.RenameColumn(
                name: "Parity",
                table: "ConfiguracionesBascula",
                newName: "Paridad");

            migrationBuilder.RenameColumn(
                name: "Handshake",
                table: "ConfiguracionesBascula",
                newName: "ControlFlujo");

            migrationBuilder.AlterColumn<int>(
                name: "TimeoutLectura",
                table: "ConfiguracionesBascula",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1000,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 2000);

            migrationBuilder.AlterColumn<bool>(
                name: "RequiereSolicitudPeso",
                table: "ConfiguracionesBascula",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "PatronExtraccion",
                table: "ConfiguracionesBascula",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 200,
                oldDefaultValue: "(\\d+\\.?\\d*)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaActualizacion",
                table: "ConfiguracionesBascula",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "datetime('now')",
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true,
                oldDefaultValueSql: "datetime('now')");

            migrationBuilder.AddColumn<string>(
                name: "ComandoInicializacion",
                table: "ConfiguracionesBascula",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TerminadorComando",
                table: "ConfiguracionesBascula",
                type: "TEXT",
                maxLength: 5,
                nullable: false,
                defaultValue: "\r\n");

            migrationBuilder.AddColumn<int>(
                name: "TimeoutEscritura",
                table: "ConfiguracionesBascula",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1000);
        }
    }
}
