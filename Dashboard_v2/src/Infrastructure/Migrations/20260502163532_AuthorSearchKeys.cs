using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuthorSearchKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstNameKey",
                table: "Authors",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastNameKey",
                table: "Authors",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SearchKey",
                table: "Authors",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            // Backfill: populate keys for existing rows using lower() as best-effort.
            // New rows will have proper accent-stripped keys via Author.Create().
            migrationBuilder.Sql("""
                UPDATE "Authors"
                SET "SearchKey"   = lower("Name"),
                    "LastNameKey" = lower("LastName"),
                    "FirstNameKey" = CASE WHEN "FirstName" IS NULL THEN NULL ELSE lower("FirstName") END
                WHERE "SearchKey" = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstNameKey",
                table: "Authors");

            migrationBuilder.DropColumn(
                name: "LastNameKey",
                table: "Authors");

            migrationBuilder.DropColumn(
                name: "SearchKey",
                table: "Authors");
        }
    }
}
