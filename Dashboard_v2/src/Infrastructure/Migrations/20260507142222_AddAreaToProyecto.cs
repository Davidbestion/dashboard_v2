using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAreaToProyecto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: add nullable so existing rows receive NULL instead of an empty string.
            migrationBuilder.AddColumn<string>(
                name: "AreaId",
                table: "Proyectos",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            // Step 2: try to assign existing projects to the first available Area.
            migrationBuilder.Sql(
                """
                UPDATE "Proyectos"
                SET    "AreaId" = (SELECT "Id" FROM "Areas" LIMIT 1)
                WHERE  "AreaId" IS NULL
                  AND  EXISTS (SELECT 1 FROM "Areas");
                """);

            // Step 3: remove projects that still have no Area (no Areas existed at migration time).
            // The derived TPT tables (ProyectosEnRevision, ProyectosEmpresariales, …)
            // have CASCADE DELETE on their FK → base-table rows, so this is safe.
            migrationBuilder.Sql(
                """
                DELETE FROM "Proyectos" WHERE "AreaId" IS NULL;
                """);

            // Step 4: now that every remaining row has a valid AreaId, enforce NOT NULL.
            migrationBuilder.AlterColumn<string>(
                name: "AreaId",
                table: "Proyectos",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldNullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
