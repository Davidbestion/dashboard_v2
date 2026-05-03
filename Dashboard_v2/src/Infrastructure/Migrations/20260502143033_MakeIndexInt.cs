using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeIndexInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL requires both changes in a single ALTER TABLE so that
            // DROP NOT NULL is applied before the USING expression can produce NULLs.
            migrationBuilder.Sql(@"
                ALTER TABLE ""IndexedPublications""
                    ALTER COLUMN ""Index"" DROP NOT NULL,
                    ALTER COLUMN ""Index"" SET DATA TYPE integer
                        USING CASE WHEN ""Index"" ~ '^[0-9]+$' THEN ""Index""::integer ELSE NULL END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Index",
                table: "IndexedPublications",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
