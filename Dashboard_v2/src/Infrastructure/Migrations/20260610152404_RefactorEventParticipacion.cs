using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorEventParticipacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Presentations_Events_EventId",
                table: "Presentations");

            migrationBuilder.DropTable(
                name: "AuthorPresentations");

            migrationBuilder.DropTable(
                name: "EventAreasPatrocinio");

            migrationBuilder.DropIndex(
                name: "IX_Presentations_EventId",
                table: "Presentations");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "Presentations");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Presentations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.CreateTable(
                name: "EventOrganizadores",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventOrganizadores", x => new { x.EventId, x.UserId });
                    table.ForeignKey(
                        name: "FK_EventOrganizadores_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventOrganizadores_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParticipacionesEnEvento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticipacionesEnEvento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParticipacionesEnEvento_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParticipacionesEnEvento_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventOrganizadores_UserId",
                table: "EventOrganizadores",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipacionesEnEvento_EventId",
                table: "ParticipacionesEnEvento",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_ParticipacionesEnEvento_UserId",
                table: "ParticipacionesEnEvento",
                column: "UserId");

            // Existing Presentations rows have no matching row in ParticipacionesEnEvento
            // (old schema lacked UserId/Fecha on Presentation directly).
            // Clearing them allows the TPT FK to be created cleanly.
            migrationBuilder.Sql("DELETE FROM \"Presentations\";");

            migrationBuilder.AddForeignKey(
                name: "FK_Presentations_ParticipacionesEnEvento_Id",
                table: "Presentations",
                column: "Id",
                principalTable: "ParticipacionesEnEvento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Presentations_ParticipacionesEnEvento_Id",
                table: "Presentations");

            migrationBuilder.DropTable(
                name: "EventOrganizadores");

            migrationBuilder.DropTable(
                name: "ParticipacionesEnEvento");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Presentations",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "Presentations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AuthorPresentations",
                columns: table => new
                {
                    AuthorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    PresentationId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthorPresentations", x => new { x.AuthorId, x.PresentationId });
                    table.ForeignKey(
                        name: "FK_AuthorPresentations_Authors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Authors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuthorPresentations_Presentations_PresentationId",
                        column: x => x.PresentationId,
                        principalTable: "Presentations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventAreasPatrocinio",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    AreaId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAreasPatrocinio", x => new { x.EventId, x.AreaId });
                    table.ForeignKey(
                        name: "FK_EventAreasPatrocinio_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventAreasPatrocinio_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Presentations_EventId",
                table: "Presentations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthorPresentations_PresentationId",
                table: "AuthorPresentations",
                column: "PresentationId");

            migrationBuilder.CreateIndex(
                name: "IX_EventAreasPatrocinio_AreaId",
                table: "EventAreasPatrocinio",
                column: "AreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Presentations_Events_EventId",
                table: "Presentations",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
