using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace costbenefi.Migrations
{
    public partial class AgregarSistemaUsuarios : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombreUsuario = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NombreCompleto = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: ""),
                    Rol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Vendedor"),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    UltimoAcceso = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IntentosFallidos = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    FechaBloqueado = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    FechaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UsuarioCreador = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    FechaCierre = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "127.0.0.1"),
                    NombreMaquina = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: ""),
                    VersionApp = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "1.0.0"),
                    UltimaActividad = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    MotivoCierre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Activo",
                table: "Users",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_FechaCreacion",
                table: "Users",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NombreUsuario",
                table: "Users",
                column: "NombreUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Rol",
                table: "Users",
                column: "Rol");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_FechaCierre",
                table: "UserSessions",
                column: "FechaCierre");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_FechaInicio",
                table: "UserSessions",
                column: "FechaInicio");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_SessionToken",
                table: "UserSessions",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
