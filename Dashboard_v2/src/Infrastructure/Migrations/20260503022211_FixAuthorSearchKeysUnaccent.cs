using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixAuthorSearchKeysUnaccent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable unaccent extension (idempotent — safe to run multiple times).
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");

            // Recompute the search keys that were previously backfilled with plain SQL
            // lower(), which does not strip diacritics.  Now we use lower(unaccent(...))
            // to produce the same output as TextNormalizer.Normalize() in C#.
            migrationBuilder.Sql("""
                UPDATE "Authors"
                SET "SearchKey"    = lower(unaccent("Name")),
                    "LastNameKey"  = lower(unaccent("LastName")),
                    "FirstNameKey" = CASE
                                        WHEN "FirstName" IS NOT NULL AND "FirstName" <> ''
                                        THEN lower(unaccent("FirstName"))
                                        ELSE NULL
                                     END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The original backfill used plain lower(); restore that approximation.
            migrationBuilder.Sql("""
                UPDATE "Authors"
                SET "SearchKey"    = lower("Name"),
                    "LastNameKey"  = lower("LastName"),
                    "FirstNameKey" = CASE
                                        WHEN "FirstName" IS NOT NULL AND "FirstName" <> ''
                                        THEN lower("FirstName")
                                        ELSE NULL
                                     END;
                """);
        }
    }
}
