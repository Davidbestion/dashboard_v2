using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migration: Adds nullable FechaInicio and FechaFin (DateOnly) columns to the Events table to record event start and end dates.
            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaFin",
                table: "Events",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "FechaInicio",
                table: "Events",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaFin",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "FechaInicio",
                table: "Events");
        }
    }
}
