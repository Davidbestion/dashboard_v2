using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorRedSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RedesCoordinadas");

            migrationBuilder.DropTable(
                name: "RedsUsuarios");

            migrationBuilder.AddColumn<string>(
                name: "CoordinadorId",
                table: "Reds",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ParticipacionesEnRed",
                columns: table => new
                {
                    RedId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AuthorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipacionesEnRed", x => new { x.RedId, x.AuthorId });
                    table.ForeignKey(
                        name: "FK_ParticipacionesEnRed_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParticipacionesEnRed_Reds_RedId",
                        column: x => x.RedId,
                        principalTable: "Reds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reds_CoordinadorId",
                table: "Reds",
                column: "CoordinadorId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipacionesEnRed_AuthorId",
                table: "ParticipacionesEnRed",
                column: "AuthorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reds_Users_CoordinadorId",
                table: "Reds",
                column: "CoordinadorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reds_Users_CoordinadorId",
                table: "Reds");

            migrationBuilder.DropTable(
                name: "ParticipacionesEnRed");

            migrationBuilder.DropIndex(
                name: "IX_Reds_CoordinadorId",
                table: "Reds");

            migrationBuilder.DropColumn(
                name: "CoordinadorId",
                table: "Reds");

            migrationBuilder.CreateTable(
                name: "RedesCoordinadas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AreaId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CoordinadorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RedId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
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

            migrationBuilder.CreateTable(
                name: "RedsUsuarios",
                columns: table => new
                {
                    RedId = table.Column<string>(type: "character varying(450)", nullable: false),
                    UsuarioId = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedsUsuarios", x => new { x.RedId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_RedsUsuarios_Reds_RedId",
                        column: x => x.RedId,
                        principalTable: "Reds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RedsUsuarios_Users_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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

            migrationBuilder.CreateIndex(
                name: "IX_RedsUsuarios_UsuarioId",
                table: "RedsUsuarios",
                column: "UsuarioId");
        }
    }
}
