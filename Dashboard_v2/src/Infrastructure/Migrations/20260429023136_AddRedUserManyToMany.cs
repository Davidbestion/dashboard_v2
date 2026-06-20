using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRedUserManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reds_Countries_CountryId",
                table: "Reds");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Reds",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Reds",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "RedId",
                table: "Events",
                type: "character varying(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "RedsUsuarios",
                columns: table => new
                {
                    RedId = table.Column<string>(type: "character varying(450)", nullable: false),
                    UsuarioId = table.Column<string>(type: "character varying(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RedsUsuarios", x => new { x.RedId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_RedsUsuarios_Reds_RedId",
                        column: x => x.RedId,
                        principalTable: "Reds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RedsUsuarios_Users_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RedsUsuarios_UsuarioId",
                table: "RedsUsuarios",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reds_Countries_CountryId",
                table: "Reds",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reds_Countries_CountryId",
                table: "Reds");

            migrationBuilder.DropTable(
                name: "RedsUsuarios");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Reds",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "Reds",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "RedId",
                table: "Events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reds_Countries_CountryId",
                table: "Reds",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id");
        }
    }
}
