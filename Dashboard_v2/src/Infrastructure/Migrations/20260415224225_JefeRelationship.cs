using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class JefeRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorreoJefe",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "Jefe",
                table: "Proyectos");

            migrationBuilder.AddColumn<string>(
                name: "JefeId",
                table: "Proyectos",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_JefeId",
                table: "Proyectos",
                column: "JefeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Proyectos_Users_JefeId",
                table: "Proyectos",
                column: "JefeId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proyectos_Users_JefeId",
                table: "Proyectos");

            migrationBuilder.DropIndex(
                name: "IX_Proyectos_JefeId",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "JefeId",
                table: "Proyectos");

            migrationBuilder.AddColumn<string>(
                name: "CorreoJefe",
                table: "Proyectos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Jefe",
                table: "Proyectos",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
