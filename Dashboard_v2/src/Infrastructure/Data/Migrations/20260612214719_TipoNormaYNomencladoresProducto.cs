using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dashboard_v2.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TipoNormaYNomencladoresProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Normas");

            migrationBuilder.AddColumn<int>(
                name: "TipoNormaId",
                table: "Normas",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TiposNorma",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposNorma", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Normas_TipoNormaId",
                table: "Normas",
                column: "TipoNormaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Normas_TiposNorma_TipoNormaId",
                table: "Normas",
                column: "TipoNormaId",
                principalTable: "TiposNorma",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Normas_TiposNorma_TipoNormaId",
                table: "Normas");

            migrationBuilder.DropTable(
                name: "TiposNorma");

            migrationBuilder.DropIndex(
                name: "IX_Normas_TipoNormaId",
                table: "Normas");

            migrationBuilder.DropColumn(
                name: "TipoNormaId",
                table: "Normas");

            migrationBuilder.AddColumn<string>(
                name: "Tipo",
                table: "Normas",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
