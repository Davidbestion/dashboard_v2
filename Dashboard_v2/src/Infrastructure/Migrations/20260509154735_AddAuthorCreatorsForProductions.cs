using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorCreatorsForProductions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthorNormas",
                columns: table => new
                {
                    AuthorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    NormaId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorNormas", x => new { x.AuthorId, x.NormaId });
                    table.ForeignKey(
                        name: "FK_AuthorNormas_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthorNormas_Normas_NormaId",
                        column: x => x.NormaId,
                        principalTable: "Normas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuthorPatentes",
                columns: table => new
                {
                    AuthorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    PatenteId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorPatentes", x => new { x.AuthorId, x.PatenteId });
                    table.ForeignKey(
                        name: "FK_AuthorPatentes_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthorPatentes_Patentes_PatenteId",
                        column: x => x.PatenteId,
                        principalTable: "Patentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuthorProductosComercializados",
                columns: table => new
                {
                    AuthorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ProductoComercializadoId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorProductosComercializados", x => new { x.AuthorId, x.ProductoComercializadoId });
                    table.ForeignKey(
                        name: "FK_AuthorProductosComercializados_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthorProductosComercializados_ProductosComercializados_Pro~",
                        column: x => x.ProductoComercializadoId,
                        principalTable: "ProductosComercializados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuthorRegistros",
                columns: table => new
                {
                    AuthorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RegistroId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorRegistros", x => new { x.AuthorId, x.RegistroId });
                    table.ForeignKey(
                        name: "FK_AuthorRegistros_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthorRegistros_Registros_RegistroId",
                        column: x => x.RegistroId,
                        principalTable: "Registros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorNormas_NormaId",
                table: "AuthorNormas",
                column: "NormaId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorPatentes_PatenteId",
                table: "AuthorPatentes",
                column: "PatenteId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorProductosComercializados_ProductoComercializadoId",
                table: "AuthorProductosComercializados",
                column: "ProductoComercializadoId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorRegistros_RegistroId",
                table: "AuthorRegistros",
                column: "RegistroId");

            migrationBuilder.Sql("""
                INSERT INTO "Authors" ("Id", "LastName", "FirstName", "Name", "SearchKey", "LastNameKey", "FirstNameKey", "UserId")
                SELECT
                    'legacy-' || u."Id",
                    COALESCE(NULLIF(u."UserLastName1", ''), u."UserName"),
                    NULLIF(u."UserName", ''),
                    CASE
                        WHEN COALESCE(NULLIF(u."UserName", ''), '') = '' THEN COALESCE(NULLIF(u."UserLastName1", ''), u."UserName")
                        ELSE COALESCE(NULLIF(u."UserLastName1", ''), u."UserName") || ', ' || u."UserName"
                    END,
                    lower(
                        CASE
                            WHEN COALESCE(NULLIF(u."UserName", ''), '') = '' THEN COALESCE(NULLIF(u."UserLastName1", ''), u."UserName")
                            ELSE COALESCE(NULLIF(u."UserLastName1", ''), u."UserName") || ', ' || u."UserName"
                        END
                    ),
                    lower(COALESCE(NULLIF(u."UserLastName1", ''), u."UserName")),
                    CASE WHEN COALESCE(NULLIF(u."UserName", ''), '') = '' THEN NULL ELSE lower(u."UserName") END,
                    u."Id"
                FROM "Users" u
                WHERE EXISTS (
                    SELECT 1 FROM "UserRegistros" ur WHERE ur."UserId" = u."Id"
                    UNION
                    SELECT 1 FROM "UserNormas" un WHERE un."UserId" = u."Id"
                    UNION
                    SELECT 1 FROM "UserProductosComercializados" up WHERE up."UserId" = u."Id"
                    UNION
                    SELECT 1 FROM "UserPatentes" upt WHERE upt."UserId" = u."Id"
                )
                AND NOT EXISTS (SELECT 1 FROM "Authors" a WHERE a."UserId" = u."Id")
                ON CONFLICT DO NOTHING;
            """);

            migrationBuilder.Sql("""
                INSERT INTO "AuthorRegistros" ("AuthorId", "RegistroId")
                SELECT a."Id", ur."RegistroId"
                FROM "UserRegistros" ur
                INNER JOIN "Authors" a ON a."UserId" = ur."UserId"
                ON CONFLICT DO NOTHING;
            """);

            migrationBuilder.Sql("""
                INSERT INTO "AuthorNormas" ("AuthorId", "NormaId")
                SELECT a."Id", un."NormaId"
                FROM "UserNormas" un
                INNER JOIN "Authors" a ON a."UserId" = un."UserId"
                ON CONFLICT DO NOTHING;
            """);

            migrationBuilder.Sql("""
                INSERT INTO "AuthorProductosComercializados" ("AuthorId", "ProductoComercializadoId")
                SELECT a."Id", up."ProductoComercializadoId"
                FROM "UserProductosComercializados" up
                INNER JOIN "Authors" a ON a."UserId" = up."UserId"
                ON CONFLICT DO NOTHING;
            """);

            migrationBuilder.Sql("""
                INSERT INTO "AuthorPatentes" ("AuthorId", "PatenteId")
                SELECT a."Id", up."PatenteId"
                FROM "UserPatentes" up
                INNER JOIN "Authors" a ON a."UserId" = up."UserId"
                ON CONFLICT DO NOTHING;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthorNormas");

            migrationBuilder.DropTable(
                name: "AuthorPatentes");

            migrationBuilder.DropTable(
                name: "AuthorProductosComercializados");

            migrationBuilder.DropTable(
                name: "AuthorRegistros");
        }
    }
}
