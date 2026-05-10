using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropUserProduccionJoinTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserNormas");

            migrationBuilder.DropTable(
                name: "UserPatentes");

            migrationBuilder.DropTable(
                name: "UserProductosComercializados");

            migrationBuilder.DropTable(
                name: "UserRegistros");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserNormas",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    NormaId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNormas", x => new { x.UserId, x.NormaId });
                    table.ForeignKey(
                        name: "FK_UserNormas_Normas_NormaId",
                        column: x => x.NormaId,
                        principalTable: "Normas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserNormas_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPatentes",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    PatenteId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPatentes", x => new { x.UserId, x.PatenteId });
                    table.ForeignKey(
                        name: "FK_UserPatentes_Patentes_PatenteId",
                        column: x => x.PatenteId,
                        principalTable: "Patentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPatentes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProductosComercializados",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ProductoComercializadoId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProductosComercializados", x => new { x.UserId, x.ProductoComercializadoId });
                    table.ForeignKey(
                        name: "FK_UserProductosComercializados_ProductosComercializados_Produ~",
                        column: x => x.ProductoComercializadoId,
                        principalTable: "ProductosComercializados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProductosComercializados_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRegistros",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RegistroId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRegistros", x => new { x.UserId, x.RegistroId });
                    table.ForeignKey(
                        name: "FK_UserRegistros_Registros_RegistroId",
                        column: x => x.RegistroId,
                        principalTable: "Registros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRegistros_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserNormas_NormaId",
                table: "UserNormas",
                column: "NormaId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPatentes_PatenteId",
                table: "UserPatentes",
                column: "PatenteId");

            migrationBuilder.CreateIndex(
                name: "IX_UserProductosComercializados_ProductoComercializadoId",
                table: "UserProductosComercializados",
                column: "ProductoComercializadoId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRegistros_RegistroId",
                table: "UserRegistros",
                column: "RegistroId");
        }
    }
}
