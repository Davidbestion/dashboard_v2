using Dashboard_v2.Application.Documents;
using Dashboard_v2.Web.Infrastructure;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// Endpoints para la generación y descarga de documentos Excel.
/// Todos requieren rol Superuser o Jefe_de_Grupo_de_investigacion.
///
/// URL genérica: GET /api/Documents/{reportName}
/// El valor de {reportName} debe coincidir con <see cref="IDocumentReport.ReportName"/>
/// de algún reporte registrado en el contenedor de dependencias.
/// Ejemplos: GET /api/Documents/anexo-grupos
/// </summary>
public class Documents : EndpointGroupBase
{
    private const string ExcelContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("{reportName}", GetDocument)
            .RequireAuthorization(p => p.RequireRole(
                nameof(RolesEnum.Superuser),
                nameof(RolesEnum.Jefe_de_Grupo_de_investigacion)))
            .WithName("GetDocument")
            .Produces(200)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404);
    }

    private static async Task<IResult> GetDocument(string reportName, IDocumentService documentService, HttpContext httpContext)
    {
        if (reportName.Equals("anexo-publicaciones", StringComparison.OrdinalIgnoreCase) &&
            !httpContext.User.IsInRole(nameof(RolesEnum.Superuser)))
        {
            return Results.Forbid();
        }

        try
        {
            var bytes = await documentService.GenerateAsync(reportName);
            var fileName = $"{reportName}_{DateTime.UtcNow:yyyy-MM}.xlsx";
            return Results.File(bytes, ExcelContentType, fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }
}
