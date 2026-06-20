using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRedCoordinadaEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RedesCoordinadas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RedId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AreaId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CoordinadorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedesCoordinadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RedesCoordinadas_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RedesCoordinadas_Reds_RedId",
                        column: x => x.RedId,
                        principalTable: "Reds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RedesCoordinadas_Users_CoordinadorId",
                        column: x => x.CoordinadorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RedesCoordinadas_AreaId",
                table: "RedesCoordinadas",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_RedesCoordinadas_CoordinadorId",
                table: "RedesCoordinadas",
                column: "CoordinadorId");

            migrationBuilder.CreateIndex(
                name: "IX_RedesCoordinadas_RedId_AreaId",
                table: "RedesCoordinadas",
                columns: new[] { "RedId", "AreaId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RedesCoordinadas");
        }
    }
}
