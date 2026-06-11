using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NomencladorRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinanciamientoUH",
                table: "ProyectosPNAP");

            migrationBuilder.DropColumn(
                name: "EntidadNoEmpresarial",
                table: "ProyectosNoEmpresariales");

            migrationBuilder.DropColumn(
                name: "Situacion",
                table: "ProyectosEnRevision");

            migrationBuilder.DropColumn(
                name: "ContribucionEjesEstrategicos",
                table: "ProyectosEnEjecucion");

            migrationBuilder.DropColumn(
                name: "ContribucionSectoresEstrategicos",
                table: "ProyectosEnEjecucion");

            migrationBuilder.DropColumn(
                name: "EntidadEjecutoraParticipante",
                table: "ProyectosEnEjecucion");

            migrationBuilder.DropColumn(
                name: "EntidadEjecutoraPrincipal",
                table: "ProyectosEnEjecucion");

            migrationBuilder.DropColumn(
                name: "EstadoDeEjecucion",
                table: "ProyectosEnEjecucion");

            migrationBuilder.DropColumn(
                name: "Empresa",
                table: "ProyectosEmpresariales");

            migrationBuilder.DropColumn(
                name: "Municipio",
                table: "ProyectosDesarrolloLocal");

            migrationBuilder.DropColumn(
                name: "FuenteFinanciacion",
                table: "ProyectosColaboracionInternacional");

            migrationBuilder.DropColumn(
                name: "NombrePrograma",
                table: "ProyectosApoyoPrograma");

            migrationBuilder.AddColumn<int>(
                name: "MunicipioId",
                table: "ProyectosDesarrolloLocal",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EjesEstrategicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EjesEstrategicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EstadosProyecto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadosProyecto", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FuentesFinanciacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuentesFinanciacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Programas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Provincias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Provincias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProyectoEmpresas",
                columns: table => new
                {
                    EmpresasId = table.Column<string>(type: "character varying(450)", nullable: false),
                    ProyectoEmpresarialId = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectoEmpresas", x => new { x.EmpresasId, x.ProyectoEmpresarialId });
                    table.ForeignKey(
                        name: "FK_ProyectoEmpresas_Institutions_EmpresasId",
                        column: x => x.EmpresasId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProyectoEmpresas_ProyectosEmpresariales_ProyectoEmpresarial~",
                        column: x => x.ProyectoEmpresarialId,
                        principalTable: "ProyectosEmpresariales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProyectoEntidades",
                columns: table => new
                {
                    EntidadesId = table.Column<string>(type: "character varying(450)", nullable: false),
                    ProyectoNoEmpresarialId = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectoEntidades", x => new { x.EntidadesId, x.ProyectoNoEmpresarialId });
                    table.ForeignKey(
                        name: "FK_ProyectoEntidades_Institutions_EntidadesId",
                        column: x => x.EntidadesId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProyectoEntidades_ProyectosNoEmpresariales_ProyectoNoEmpres~",
                        column: x => x.ProyectoNoEmpresarialId,
                        principalTable: "ProyectosNoEmpresariales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProyectoEntidadesParticipantes",
                columns: table => new
                {
                    EntidadesEjecutorasParticipantesId = table.Column<string>(type: "character varying(450)", nullable: false),
                    ProyectoEnEjecucion1Id = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectoEntidadesParticipantes", x => new { x.EntidadesEjecutorasParticipantesId, x.ProyectoEnEjecucion1Id });
                    table.ForeignKey(
                        name: "FK_ProyectoEntidadesParticipantes_Institutions_EntidadesEjecut~",
                        column: x => x.EntidadesEjecutorasParticipantesId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProyectoEntidadesParticipantes_ProyectosEnEjecucion_Proyect~",
                        column: x => x.ProyectoEnEjecucion1Id,
                        principalTable: "ProyectosEnEjecucion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProyectoEntidadesPrincipales",
                columns: table => new
                {
                    EntidadesEjecutorasPrincipalesId = table.Column<string>(type: "character varying(450)", nullable: false),
                    ProyectoEnEjecucionId = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectoEntidadesPrincipales", x => new { x.EntidadesEjecutorasPrincipalesId, x.ProyectoEnEjecucionId });
                    table.ForeignKey(
                        name: "FK_ProyectoEntidadesPrincipales_Institutions_EntidadesEjecutor~",
                        column: x => x.EntidadesEjecutorasPrincipalesId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProyectoEntidadesPrincipales_ProyectosEnEjecucion_ProyectoE~",
                        column: x => x.ProyectoEnEjecucionId,
                        principalTable: "ProyectosEnEjecucion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SectoresEstrategicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectoresEstrategicos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SituacionesProyecto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SituacionesProyecto", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProyectoEjes",
                columns: table => new
                {
                    EjesEstrategicosId = table.Column<int>(type: "integer", nullable: false),
                    ProyectoEnEjecucionId = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectoEjes", x => new { x.EjesEstrategicosId, x.ProyectoEnEjecucionId });
                    table.ForeignKey(
                        name: "FK_ProyectoEjes_EjesEstrategicos_EjesEstrategicosId",
                        column: x => x.EjesEstrategicosId,
                        principalTable: "EjesEstrategicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProyectoEjes_ProyectosEnEjecucion_ProyectoEnEjecucionId",
                        column: x => x.ProyectoEnEjecucionId,
                        principalTable: "ProyectosEnEjecucion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProyectoEstados",
                columns: table => new
                {
                    EstadosDeEjecucionId = table.Column<int>(type: "integer", nullable: false),
                    ProyectoEnEjecucionId = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectoEstados", x => new { x.EstadosDeEjecucionId, x.ProyectoEnEjecucionId });
                    table.ForeignKey(
                        name: "FK_ProyectoEstados_EstadosProyecto_EstadosDeEjecucionId",
                        column: x => x.EstadosDeEjecucionId,
                        principalTable: "EstadosProyecto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProyectoEstados_ProyectosEnEjecucion_ProyectoEnEjecucionId",
                        column: x => x.ProyectoEnEjecucionId,
                        principalTable: "ProyectosEnEjecucion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProyectoPNAPFuentes",
                columns: table => new
                {
                    FuentesFinanciacionId = table.Column<int>(type: "integer", nullable: false),
                    ProyectoPNAPId = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectoPNAPFuentes", x => new { x.FuentesFinanciacionId, x.ProyectoPNAPId });
                    table.ForeignKey(
                        name: "FK_ProyectoPNAPFuentes_FuentesFinanciacion_FuentesFinanciacion~",
                        column: x => x.FuentesFinanciacionId,
                        principalTable: "FuentesFinanciacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProyectoPNAPFuentes_ProyectosPNAP_ProyectoPNAPId",
                        column: x => x.ProyectoPNAPId,
                        principalTable: "ProyectosPNAP",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProyectoPRCIFuentes",
                columns: table => new
                {
                    FuentesFinanciacionId = table.Column<int>(type: "integer", nullable: false),
                    ProyectoColabInternacionalId = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectoPRCIFuentes", x => new { x.FuentesFinanciacionId, x.ProyectoColabInternacionalId });
                    table.ForeignKey(
                        name: "FK_ProyectoPRCIFuentes_FuentesFinanciacion_FuentesFinanciacion~",
                        column: x => x.FuentesFinanciacionId,
                        principalTable: "FuentesFinanciacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProyectoPRCIFuentes_ProyectosColaboracionInternacional_Proy~",
                        column: x => x.ProyectoColabInternacionalId,
                        principalTable: "ProyectosColaboracionInternacional",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProyectoProgramas",
                columns: table => new
                {
                    ProgramasId = table.Column<int>(type: "integer", nullable: false),
                    ProyectoApoyoProgramaId = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectoProgramas", x => new { x.ProgramasId, x.ProyectoApoyoProgramaId });
                    table.ForeignKey(
                        name: "FK_ProyectoProgramas_Programas_ProgramasId",
                        column: x => x.ProgramasId,
                        principalTable: "Programas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProyectoProgramas_ProyectosApoyoPrograma_ProyectoApoyoProgr~",
                        column: x => x.ProyectoApoyoProgramaId,
                        principalTable: "ProyectosApoyoPrograma",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Municipios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProvinciaId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Municipios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Municipios_Provincias_ProvinciaId",
                        column: x => x.ProvinciaId,
                        principalTable: "Provincias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProyectoSectores",
                columns: table => new
                {
                    ProyectoEnEjecucionId = table.Column<string>(type: "character varying(450)", nullable: false),
                    SectoresEstrategicosId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectoSectores", x => new { x.ProyectoEnEjecucionId, x.SectoresEstrategicosId });
                    table.ForeignKey(
                        name: "FK_ProyectoSectores_ProyectosEnEjecucion_ProyectoEnEjecucionId",
                        column: x => x.ProyectoEnEjecucionId,
                        principalTable: "ProyectosEnEjecucion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProyectoSectores_SectoresEstrategicos_SectoresEstrategicosId",
                        column: x => x.SectoresEstrategicosId,
                        principalTable: "SectoresEstrategicos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProyectoRevisionSituaciones",
                columns: table => new
                {
                    ProyectoEnRevisionId = table.Column<string>(type: "character varying(450)", nullable: false),
                    SituacionesId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectoRevisionSituaciones", x => new { x.ProyectoEnRevisionId, x.SituacionesId });
                    table.ForeignKey(
                        name: "FK_ProyectoRevisionSituaciones_ProyectosEnRevision_ProyectoEnR~",
                        column: x => x.ProyectoEnRevisionId,
                        principalTable: "ProyectosEnRevision",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProyectoRevisionSituaciones_SituacionesProyecto_Situaciones~",
                        column: x => x.SituacionesId,
                        principalTable: "SituacionesProyecto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProyectosDesarrolloLocal_MunicipioId",
                table: "ProyectosDesarrolloLocal",
                column: "MunicipioId");

            migrationBuilder.CreateIndex(
                name: "IX_Municipios_ProvinciaId",
                table: "Municipios",
                column: "ProvinciaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoEjes_ProyectoEnEjecucionId",
                table: "ProyectoEjes",
                column: "ProyectoEnEjecucionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoEmpresas_ProyectoEmpresarialId",
                table: "ProyectoEmpresas",
                column: "ProyectoEmpresarialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoEntidades_ProyectoNoEmpresarialId",
                table: "ProyectoEntidades",
                column: "ProyectoNoEmpresarialId");

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoEntidadesParticipantes_ProyectoEnEjecucion1Id",
                table: "ProyectoEntidadesParticipantes",
                column: "ProyectoEnEjecucion1Id");

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoEntidadesPrincipales_ProyectoEnEjecucionId",
                table: "ProyectoEntidadesPrincipales",
                column: "ProyectoEnEjecucionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoEstados_ProyectoEnEjecucionId",
                table: "ProyectoEstados",
                column: "ProyectoEnEjecucionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoPNAPFuentes_ProyectoPNAPId",
                table: "ProyectoPNAPFuentes",
                column: "ProyectoPNAPId");

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoPRCIFuentes_ProyectoColabInternacionalId",
                table: "ProyectoPRCIFuentes",
                column: "ProyectoColabInternacionalId");

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoProgramas_ProyectoApoyoProgramaId",
                table: "ProyectoProgramas",
                column: "ProyectoApoyoProgramaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoRevisionSituaciones_SituacionesId",
                table: "ProyectoRevisionSituaciones",
                column: "SituacionesId");

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoSectores_SectoresEstrategicosId",
                table: "ProyectoSectores",
                column: "SectoresEstrategicosId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProyectosDesarrolloLocal_Municipios_MunicipioId",
                table: "ProyectosDesarrolloLocal",
                column: "MunicipioId",
                principalTable: "Municipios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProyectosDesarrolloLocal_Municipios_MunicipioId",
                table: "ProyectosDesarrolloLocal");

            migrationBuilder.DropTable(
                name: "Municipios");

            migrationBuilder.DropTable(
                name: "ProyectoEjes");

            migrationBuilder.DropTable(
                name: "ProyectoEmpresas");

            migrationBuilder.DropTable(
                name: "ProyectoEntidades");

            migrationBuilder.DropTable(
                name: "ProyectoEntidadesParticipantes");

            migrationBuilder.DropTable(
                name: "ProyectoEntidadesPrincipales");

            migrationBuilder.DropTable(
                name: "ProyectoEstados");

            migrationBuilder.DropTable(
                name: "ProyectoPNAPFuentes");

            migrationBuilder.DropTable(
                name: "ProyectoPRCIFuentes");

            migrationBuilder.DropTable(
                name: "ProyectoProgramas");

            migrationBuilder.DropTable(
                name: "ProyectoRevisionSituaciones");

            migrationBuilder.DropTable(
                name: "ProyectoSectores");

            migrationBuilder.DropTable(
                name: "Provincias");

            migrationBuilder.DropTable(
                name: "EjesEstrategicos");

            migrationBuilder.DropTable(
                name: "EstadosProyecto");

            migrationBuilder.DropTable(
                name: "FuentesFinanciacion");

            migrationBuilder.DropTable(
                name: "Programas");

            migrationBuilder.DropTable(
                name: "SituacionesProyecto");

            migrationBuilder.DropTable(
                name: "SectoresEstrategicos");

            migrationBuilder.DropIndex(
                name: "IX_ProyectosDesarrolloLocal_MunicipioId",
                table: "ProyectosDesarrolloLocal");

            migrationBuilder.DropColumn(
                name: "MunicipioId",
                table: "ProyectosDesarrolloLocal");

            migrationBuilder.AddColumn<string>(
                name: "FinanciamientoUH",
                table: "ProyectosPNAP",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EntidadNoEmpresarial",
                table: "ProyectosNoEmpresariales",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Situacion",
                table: "ProyectosEnRevision",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContribucionEjesEstrategicos",
                table: "ProyectosEnEjecucion",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContribucionSectoresEstrategicos",
                table: "ProyectosEnEjecucion",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntidadEjecutoraParticipante",
                table: "ProyectosEnEjecucion",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntidadEjecutoraPrincipal",
                table: "ProyectosEnEjecucion",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EstadoDeEjecucion",
                table: "ProyectosEnEjecucion",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Empresa",
                table: "ProyectosEmpresariales",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Municipio",
                table: "ProyectosDesarrolloLocal",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FuenteFinanciacion",
                table: "ProyectosColaboracionInternacional",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NombrePrograma",
                table: "ProyectosApoyoPrograma",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");
        }
    }
}
