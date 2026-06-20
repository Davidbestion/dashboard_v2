using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceFileToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EvidenceFileId",
                table: "UserAwardeds",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EvidenceFileId",
                table: "Registros",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EvidenceFileId",
                table: "Publications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EvidenceFileId",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAwardeds_EvidenceFileId",
                table: "UserAwardeds",
                column: "EvidenceFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Registros_EvidenceFileId",
                table: "Registros",
                column: "EvidenceFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Publications_EvidenceFileId",
                table: "Publications",
                column: "EvidenceFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_EvidenceFileId",
                table: "Events",
                column: "EvidenceFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_StoredFiles_EvidenceFileId",
                table: "Events",
                column: "EvidenceFileId",
                principalTable: "StoredFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Publications_StoredFiles_EvidenceFileId",
                table: "Publications",
                column: "EvidenceFileId",
                principalTable: "StoredFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Registros_StoredFiles_EvidenceFileId",
                table: "Registros",
                column: "EvidenceFileId",
                principalTable: "StoredFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAwardeds_StoredFiles_EvidenceFileId",
                table: "UserAwardeds",
                column: "EvidenceFileId",
                principalTable: "StoredFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_StoredFiles_EvidenceFileId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Publications_StoredFiles_EvidenceFileId",
                table: "Publications");

            migrationBuilder.DropForeignKey(
                name: "FK_Registros_StoredFiles_EvidenceFileId",
                table: "Registros");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAwardeds_StoredFiles_EvidenceFileId",
                table: "UserAwardeds");

            migrationBuilder.DropIndex(
                name: "IX_UserAwardeds_EvidenceFileId",
                table: "UserAwardeds");

            migrationBuilder.DropIndex(
                name: "IX_Registros_EvidenceFileId",
                table: "Registros");

            migrationBuilder.DropIndex(
                name: "IX_Publications_EvidenceFileId",
                table: "Publications");

            migrationBuilder.DropIndex(
                name: "IX_Events_EvidenceFileId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "EvidenceFileId",
                table: "UserAwardeds");

            migrationBuilder.DropColumn(
                name: "EvidenceFileId",
                table: "Registros");

            migrationBuilder.DropColumn(
                name: "EvidenceFileId",
                table: "Publications");

            migrationBuilder.DropColumn(
                name: "EvidenceFileId",
                table: "Events");
        }
    }
}
