using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRedCountryRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsNacional",
                table: "Reds");

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "Reds",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reds_CountryId",
                table: "Reds",
                column: "CountryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reds_Countries_CountryId",
                table: "Reds",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reds_Countries_CountryId",
                table: "Reds");

            migrationBuilder.DropIndex(
                name: "IX_Reds_CountryId",
                table: "Reds");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "Reds");

            migrationBuilder.AddColumn<bool>(
                name: "EsNacional",
                table: "Reds",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
