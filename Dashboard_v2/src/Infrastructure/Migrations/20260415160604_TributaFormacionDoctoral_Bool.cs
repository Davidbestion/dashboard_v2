using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dashboard_v2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TributaFormacionDoctoral_Bool : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Paso 1: renombrar la columna (era int con el nombre anterior)
            migrationBuilder.RenameColumn(
                name: "CantidadEstudiantesDoctorado",
                table: "Proyectos",
                newName: "TributaFormacionDoctoral");

            // Paso 2: cambiar el tipo de integer a boolean
            // PostgreSQL no hace el cast int→bool automáticamente; se requiere USING explícito.
            // 0 → false, cualquier otro valor → true.
            migrationBuilder.Sql(
                """
                ALTER TABLE "Proyectos"
                    ALTER COLUMN "TributaFormacionDoctoral" TYPE boolean
                    USING ("TributaFormacionDoctoral" <> 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "Proyectos"
                    ALTER COLUMN "TributaFormacionDoctoral" TYPE integer
                    USING ("TributaFormacionDoctoral"::int);
                """);

            migrationBuilder.RenameColumn(
                name: "TributaFormacionDoctoral",
                table: "Proyectos",
                newName: "CantidadEstudiantesDoctorado");
        }
    }
}
