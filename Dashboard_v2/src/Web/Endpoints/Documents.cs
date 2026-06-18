using Dashboard_v2.Application.Documents;
using Dashboard_v2.Web.Infrastructure;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// Endpoints para la generación y descarga de documentos Excel.
/// Cada reporte declara sus propios roles autorizados vía <see cref="IDocumentReport.AllowedRoles"/>
/// (o <see cref="IZipDocumentReport.AllowedRoles"/>); este endpoint solo exige autenticación
/// y delega la decisión de autorización al reporte resuelto por nombre.
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

    private const string ZipContentType = "application/zip";

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("{reportName}", GetDocument)
            .RequireAuthorization()
            .WithName("GetDocument")
            .Produces(200)
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404);
    }

    private static async Task<IResult> GetDocument(
        string reportName,
        IDocumentService documentService,
        HttpContext httpContext,
        [Microsoft.AspNetCore.Mvc.FromQuery] string? from = null,
        [Microsoft.AspNetCore.Mvc.FromQuery] string? to = null)
    {
        var allowedRoles = documentService.GetAllowedRoles(reportName);
        if (allowedRoles is null)
            return Results.NotFound(new { error = $"No existe un reporte registrado con el nombre '{reportName}'." });

        if (!allowedRoles.Any(httpContext.User.IsInRole))
            return Results.Forbid();

        Dictionary<string, string>? parameters = null;
        if (!string.IsNullOrWhiteSpace(from) || !string.IsNullOrWhiteSpace(to))
        {
            parameters = [];
            if (!string.IsNullOrWhiteSpace(from)) parameters["from"] = from;
            if (!string.IsNullOrWhiteSpace(to))   parameters["to"]   = to;
        }

        try
        {
            var bytes = await documentService.GenerateAsync(reportName, parameters);

            if (documentService.IsZipReport(reportName))
            {
                var zipFileName = $"{reportName}_{DateTime.UtcNow:yyyy-MM}.zip";
                return Results.File(bytes, ZipContentType, zipFileName);
            }

            var fileName = $"{reportName}_{DateTime.UtcNow:yyyy-MM}.xlsx";
            return Results.File(bytes, ExcelContentType, fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }
}
