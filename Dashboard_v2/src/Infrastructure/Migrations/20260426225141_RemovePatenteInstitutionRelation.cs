using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePatenteInstitutionRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Patentes_Institutions_InstitutionId",
                table: "Patentes");

            migrationBuilder.DropIndex(
                name: "IX_Patentes_InstitutionId",
                table: "Patentes");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "Patentes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InstitutionId",
                table: "Patentes",
                type: "character varying(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Patentes_InstitutionId",
                table: "Patentes",
                column: "InstitutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Patentes_Institutions_InstitutionId",
                table: "Patentes",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
