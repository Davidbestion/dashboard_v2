using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreadorIdToGrupo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreadorId",
                table: "GruposDeInvestigacion",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GruposDeInvestigacion_CreadorId",
                table: "GruposDeInvestigacion",
                column: "CreadorId");

            migrationBuilder.AddForeignKey(
                name: "FK_GruposDeInvestigacion_Users_CreadorId",
                table: "GruposDeInvestigacion",
                column: "CreadorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GruposDeInvestigacion_Users_CreadorId",
                table: "GruposDeInvestigacion");

            migrationBuilder.DropIndex(
                name: "IX_GruposDeInvestigacion_CreadorId",
                table: "GruposDeInvestigacion");

            migrationBuilder.DropColumn(
                name: "CreadorId",
                table: "GruposDeInvestigacion");
        }
    }
}
