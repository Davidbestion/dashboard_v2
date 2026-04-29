using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillNormalizedUrlDoi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
                        // Backfill NormalizedUrlDoi from existing UrlDoi values for legacy data.
                        // Steps: remove scheme, common doi hosts, query params, lowercase and trim trailing slashes.
                        migrationBuilder.Sql(@"
UPDATE ""Publications""
SET ""NormalizedUrlDoi"" = rtrim(
    regexp_replace(
        lower(
            regexp_replace(
                regexp_replace(
                    regexp_replace(""UrlDoi"", '^https?://', '', 'i'),
                    '^doi:', '', 'i'),
                '^(dx\.)?doi\.org/', '', 'i')
        ), '\?.*$', '', 'i')
    , '/')
WHERE ""NormalizedUrlDoi"" IS NULL AND ""UrlDoi"" IS NOT NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
