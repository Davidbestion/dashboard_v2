using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PublicacionesDerivadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProyectoId",
                table: "Publications",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Publications_ProyectoId",
                table: "Publications",
                column: "ProyectoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Publications_Proyectos_ProyectoId",
                table: "Publications",
                column: "ProyectoId",
                principalTable: "Proyectos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Publications_Proyectos_ProyectoId",
                table: "Publications");

            migrationBuilder.DropIndex(
                name: "IX_Publications_ProyectoId",
                table: "Publications");

            migrationBuilder.DropColumn(
                name: "ProyectoId",
                table: "Publications");
        }
    }
}
