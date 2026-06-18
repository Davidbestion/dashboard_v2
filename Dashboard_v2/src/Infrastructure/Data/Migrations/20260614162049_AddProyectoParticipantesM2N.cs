using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProyectoParticipantesM2N : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proyectos_Areas_AreaId",
                table: "Proyectos");

            migrationBuilder.DropIndex(
                name: "IX_Proyectos_AreaId",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "AreaId",
                table: "Proyectos");

            migrationBuilder.CreateTable(
                name: "ProyectoParticipantes",
                columns: table => new
                {
                    ParticipantesId = table.Column<string>(type: "character varying(450)", nullable: false),
                    ProyectosParticipanteId = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProyectoParticipantes", x => new { x.ParticipantesId, x.ProyectosParticipanteId });
                    table.ForeignKey(
                        name: "FK_ProyectoParticipantes_Proyectos_ProyectosParticipanteId",
                        column: x => x.ProyectosParticipanteId,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProyectoParticipantes_Users_ParticipantesId",
                        column: x => x.ParticipantesId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProyectoParticipantes_ProyectosParticipanteId",
                table: "ProyectoParticipantes",
                column: "ProyectosParticipanteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProyectoParticipantes");

            migrationBuilder.AddColumn<string>(
                name: "AreaId",
                table: "Proyectos",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_AreaId",
                table: "Proyectos",
                column: "AreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Proyectos_Areas_AreaId",
                table: "Proyectos",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
