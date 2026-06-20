using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMerEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Normas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InstitutionId = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Normas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Normas_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Patentes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    NumeroSolicitudConcesion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EsNacional = table.Column<bool>(type: "boolean", nullable: false),
                    InstitutionId = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patentes_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Registros",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    NumeroCertificado = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CountryId = table.Column<int>(type: "integer", nullable: false),
                    InstitutionId = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Registros_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Registros_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TipoProductosComercializados",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoProductosComercializados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductosComercializados",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TipoProductoComercializadoId = table.Column<string>(type: "character varying(450)", nullable: false),
                    InstitutionId = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductosComercializados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductosComercializados_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductosComercializados_TipoProductosComercializados_TipoP~",
                        column: x => x.TipoProductoComercializadoId,
                        principalTable: "TipoProductosComercializados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Normas_InstitutionId",
                table: "Normas",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Patentes_InstitutionId",
                table: "Patentes",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductosComercializados_InstitutionId",
                table: "ProductosComercializados",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductosComercializados_TipoProductoComercializadoId",
                table: "ProductosComercializados",
                column: "TipoProductoComercializadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Registros_CountryId",
                table: "Registros",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Registros_InstitutionId",
                table: "Registros",
                column: "InstitutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Normas");

            migrationBuilder.DropTable(
                name: "Patentes");

            migrationBuilder.DropTable(
                name: "ProductosComercializados");

            migrationBuilder.DropTable(
                name: "Registros");

            migrationBuilder.DropTable(
                name: "TipoProductosComercializados");
        }
    }
}
