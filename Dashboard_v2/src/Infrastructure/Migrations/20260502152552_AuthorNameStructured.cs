using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AuthorNameStructured : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Authors",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Authors",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            // Retrocompatibilidad: los autores existentes almacenaban todo en Name.
            // Se copia Name → LastName; FirstName queda nulo hasta que se corrija manualmente.
            migrationBuilder.Sql(@"UPDATE ""Authors"" SET ""LastName"" = ""Name"" WHERE ""LastName"" = ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Authors");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Authors");
        }
    }
}
