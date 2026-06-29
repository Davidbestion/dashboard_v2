using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFileDeletionJobIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FileDeletionJobs_Attempts_ScheduledAt",
                table: "FileDeletionJobs");

            migrationBuilder.CreateIndex(
                name: "IX_FileDeletionJobs_ScheduledAt",
                table: "FileDeletionJobs",
                column: "ScheduledAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FileDeletionJobs_ScheduledAt",
                table: "FileDeletionJobs");

            migrationBuilder.CreateIndex(
                name: "IX_FileDeletionJobs_Attempts_ScheduledAt",
                table: "FileDeletionJobs",
                columns: new[] { "Attempts", "ScheduledAt" });
        }
    }
}
