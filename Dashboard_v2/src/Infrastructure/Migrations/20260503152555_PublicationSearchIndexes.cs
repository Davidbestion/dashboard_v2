using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PublicationSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill NormalizedUrlDoi for rows that existed before the column was added.
            // The logic mirrors PublicationService.NormalizeUrlDoi():
            //   lower(trim(url)), remove scheme, doi.org/, dx.doi.org/, doi:, trailing ? params and slashes.
            migrationBuilder.Sql("""
                UPDATE "Publications"
                SET "NormalizedUrlDoi" = trim(trailing '/' from
                        split_part(
                            regexp_replace(
                                regexp_replace(
                                    replace(replace(replace(lower(trim("UrlDoi")), 'doi.org/', ''), 'dx.doi.org/', ''), 'doi:', ''),
                                    '^https?://', ''),
                                '\?.*$', ''),
                            '?', 1))
                WHERE "UrlDoi" IS NOT NULL AND "UrlDoi" <> ''
                  AND ("NormalizedUrlDoi" IS NULL OR "NormalizedUrlDoi" = '');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Publications_NormalizedTitle",
                table: "Publications",
                column: "NormalizedTitle");

            migrationBuilder.CreateIndex(
                name: "IX_Publications_NormalizedUrlDoi",
                table: "Publications",
                column: "NormalizedUrlDoi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Publications_NormalizedTitle",
                table: "Publications");

            migrationBuilder.DropIndex(
                name: "IX_Publications_NormalizedUrlDoi",
                table: "Publications");
        }
    }
}
