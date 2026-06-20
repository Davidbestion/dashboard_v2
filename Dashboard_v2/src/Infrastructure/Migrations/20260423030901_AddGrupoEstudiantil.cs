using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGrupoEstudiantil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GruposEstudiantiles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AreaId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposEstudiantiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GruposEstudiantiles_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GruposEstudiantilesLineasDeInvestigacion",
                columns: table => new
                {
                    GrupoEstudiantilId = table.Column<string>(type: "character varying(450)", nullable: false),
                    LineaDeInvestigacionId = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposEstudiantilesLineasDeInvestigacion", x => new { x.GrupoEstudiantilId, x.LineaDeInvestigacionId });
                    table.ForeignKey(
                        name: "FK_GruposEstudiantilesLineasDeInvestigacion_GruposEstudiantile~",
                        column: x => x.GrupoEstudiantilId,
                        principalTable: "GruposEstudiantiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GruposEstudiantilesLineasDeInvestigacion_LineasDeInvestigac~",
                        column: x => x.LineaDeInvestigacionId,
                        principalTable: "LineasDeInvestigacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GruposEstudiantiles_AreaId",
                table: "GruposEstudiantiles",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_GruposEstudiantilesLineasDeInvestigacion_LineaDeInvestigaci~",
                table: "GruposEstudiantilesLineasDeInvestigacion",
                column: "LineaDeInvestigacionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GruposEstudiantilesLineasDeInvestigacion");

            migrationBuilder.DropTable(
                name: "GruposEstudiantiles");
        }
    }
}
