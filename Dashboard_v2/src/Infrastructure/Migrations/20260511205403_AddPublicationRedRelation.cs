using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicationRedRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RedId",
                table: "Publications",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Publications_RedId",
                table: "Publications",
                column: "RedId");

            migrationBuilder.AddForeignKey(
                name: "FK_Publications_Reds_RedId",
                table: "Publications",
                column: "RedId",
                principalTable: "Reds",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Publications_Reds_RedId",
                table: "Publications");

            migrationBuilder.DropIndex(
                name: "IX_Publications_RedId",
                table: "Publications");

            migrationBuilder.DropColumn(
                name: "RedId",
                table: "Publications");
        }
    }
}
