using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRedRelationToEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RedId",
                table: "Events",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_RedId",
                table: "Events",
                column: "RedId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Reds_RedId",
                table: "Events",
                column: "RedId",
                principalTable: "Reds",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Reds_RedId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_RedId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RedId",
                table: "Events");
        }
    }
}
