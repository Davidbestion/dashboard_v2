using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BaseDeDatosPublicacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataBase",
                table: "JournalPublications");

            migrationBuilder.AddColumn<int>(
                name: "BaseDeDatosId",
                table: "JournalPublications",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BasesDeDatosPublicacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BasesDeDatosPublicacion", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalPublications_BaseDeDatosId",
                table: "JournalPublications",
                column: "BaseDeDatosId");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalPublications_BasesDeDatosPublicacion_BaseDeDatosId",
                table: "JournalPublications",
                column: "BaseDeDatosId",
                principalTable: "BasesDeDatosPublicacion",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalPublications_BasesDeDatosPublicacion_BaseDeDatosId",
                table: "JournalPublications");

            migrationBuilder.DropTable(
                name: "BasesDeDatosPublicacion");

            migrationBuilder.DropIndex(
                name: "IX_JournalPublications_BaseDeDatosId",
                table: "JournalPublications");

            migrationBuilder.DropColumn(
                name: "BaseDeDatosId",
                table: "JournalPublications");

            migrationBuilder.AddColumn<string>(
                name: "DataBase",
                table: "JournalPublications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
