using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicationSpecializations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IndexedPublications",
                columns: table => new
                {
                    PublicationId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Index = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexedPublications", x => x.PublicationId);
                    table.ForeignKey(
                        name: "FK_IndexedPublications_Publications_PublicationId",
                        column: x => x.PublicationId,
                        principalTable: "Publications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JournalPublications",
                columns: table => new
                {
                    PublicationId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DataBase = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Group = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalPublications", x => x.PublicationId);
                    table.ForeignKey(
                        name: "FK_JournalPublications_Publications_PublicationId",
                        column: x => x.PublicationId,
                        principalTable: "Publications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JournalGroup1Publications",
                columns: table => new
                {
                    PublicationId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Cuartil = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalGroup1Publications", x => x.PublicationId);
                    table.ForeignKey(
                        name: "FK_JournalGroup1Publications_JournalPublications_PublicationId",
                        column: x => x.PublicationId,
                        principalTable: "JournalPublications",
                        principalColumn: "PublicationId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndexedPublications");

            migrationBuilder.DropTable(
                name: "JournalGroup1Publications");

            migrationBuilder.DropTable(
                name: "JournalPublications");
        }
    }
}
