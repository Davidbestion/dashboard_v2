using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProyectosAndClasificaciones_TPT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clasificaciones",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clasificaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Proyectos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Jefe = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CorreoJefe = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NumeroMiembros = table.Column<int>(type: "integer", nullable: false),
                    CantidadMiembrosUH = table.Column<int>(type: "integer", nullable: false),
                    CantidadEstudiantes = table.Column<int>(type: "integer", nullable: false),
                    CantidadEstudiantesContratados = table.Column<int>(type: "integer", nullable: false),
                    TributaFormacionDoctoral = table.Column<int>(type: "integer", nullable: false),
                    ClasificacionId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proyectos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proyectos_Clasificaciones_ClasificacionId",
                        column: x => x.ClasificacionId,
                        principalTable: "Clasificaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProyectosEnEjecucion",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaCierre = table.Column<DateOnly>(type: "date", nullable: true),
                    EstadoDeEjecucion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CodigoProyecto = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntidadEjecutoraPrincipal = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EntidadEjecutoraParticipante = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContribucionSectoresEstrategicos = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ContribucionEjesEstrategicos = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectosEnEjecucion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProyectosEnEjecucion_Proyectos_Id",
                        column: x => x.Id,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProyectosEnRevision",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Situacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectosEnRevision", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProyectosEnRevision_Proyectos_Id",
                        column: x => x.Id,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProyectosApoyoPrograma",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    NombrePrograma = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TipoPAP = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectosApoyoPrograma", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProyectosApoyoPrograma_ProyectosEnEjecucion_Id",
                        column: x => x.Id,
                        principalTable: "ProyectosEnEjecucion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProyectosColaboracionInternacional",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    FuenteFinanciacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TerminosReferencia = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectosColaboracionInternacional", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProyectosColaboracionInternacional_ProyectosEnEjecucion_Id",
                        column: x => x.Id,
                        principalTable: "ProyectosEnEjecucion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProyectosDesarrolloLocal",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Municipio = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectosDesarrolloLocal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProyectosDesarrolloLocal_ProyectosEnEjecucion_Id",
                        column: x => x.Id,
                        principalTable: "ProyectosEnEjecucion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProyectosEmpresariales",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Empresa = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectosEmpresariales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProyectosEmpresariales_ProyectosEnEjecucion_Id",
                        column: x => x.Id,
                        principalTable: "ProyectosEnEjecucion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProyectosNoEmpresariales",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    EntidadNoEmpresarial = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectosNoEmpresariales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProyectosNoEmpresariales_ProyectosEnEjecucion_Id",
                        column: x => x.Id,
                        principalTable: "ProyectosEnEjecucion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProyectosPNAP",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    FinanciamientoUH = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectosPNAP", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProyectosPNAP_ProyectosEnEjecucion_Id",
                        column: x => x.Id,
                        principalTable: "ProyectosEnEjecucion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_ClasificacionId",
                table: "Proyectos",
                column: "ClasificacionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProyectosApoyoPrograma");

            migrationBuilder.DropTable(
                name: "ProyectosColaboracionInternacional");

            migrationBuilder.DropTable(
                name: "ProyectosDesarrolloLocal");

            migrationBuilder.DropTable(
                name: "ProyectosEmpresariales");

            migrationBuilder.DropTable(
                name: "ProyectosEnRevision");

            migrationBuilder.DropTable(
                name: "ProyectosNoEmpresariales");

            migrationBuilder.DropTable(
                name: "ProyectosPNAP");

            migrationBuilder.DropTable(
                name: "ProyectosEnEjecucion");

            migrationBuilder.DropTable(
                name: "Proyectos");

            migrationBuilder.DropTable(
                name: "Clasificaciones");
        }
    }
}
