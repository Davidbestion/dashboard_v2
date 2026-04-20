# 0007 — Generación de documentos Excel: Strategy + ClosedXML.Report + plantillas embebidas

## Status
Accepted

## Fecha
2026-04-18

## Contexto

El sistema debe poder generar archivos Excel descargables (anexos institucionales) a partir de los datos almacenados en la base de datos. Los requisitos concretos son:

- **Múltiples documentos distintos**: cada anexo tiene su propio formato, columnas y estructura (algunos con una sola hoja, otros con una hoja por tipo de entidad).
- **Diseño editable sin tocar código**: el formato visual (colores, cabeceras, fusiones de celdas, anchos de columna) debe poder modificarse en Excel sin recompilar ni redesplegar la aplicación.
- **Extensibilidad sin modificar código existente**: añadir un nuevo tipo de documento no debe requerir modificar ninguna clase existente (OCP).
- **Acceso controlado por rol**: solo `Superuser` y `Jefe_de_Grupo_de_investigacion` pueden generar documentos.

### Alternativas consideradas

#### A — Generar Excel completamente en código (ClosedXML puro)
Cada documento se construye programáticamente: se crea el workbook, se aplican estilos, se escriben valores.  
✗ El diseño visual vive en el código → cualquier cambio de formato requiere recompilación.  
✗ Difícil de mantener a medida que crecen el número y la complejidad de los documentos.

#### B — Plantillas físicas .xlsx + motor de plantillas (ClosedXML.Report)
Se diseña el archivo Excel con formato real. Se definen **Named Ranges** con expresiones `{{item.Propiedad}}`. En tiempo de ejecución el motor expande los rangos con los datos.  
✓ El diseño visual vive en el `.xlsx` — se puede editar con Excel.  
✓ Soporte nativo para múltiples hojas: un Named Range por hoja, completamente independientes.  
✗ Requiere dependencias adicionales y suprimir advertencias de seguridad transitivas (ver más abajo).

Se eligió **B**.

---

## Decisión

### Arquitectura general

Se implementa un **patrón Strategy** con tres piezas:

```
IDocumentReport           ← Strategy: cada documento es una clase independiente
IDocumentRenderer         ← Infraestructura: aplica la plantilla y devuelve bytes
GenerateDocumentQuery     ← CQRS: query genérico que despacha por nombre
GET /api/Documents/{name} ← Endpoint genérico
```

#### `IDocumentReport` (Application/Documents/)
Contrato que implementa cada documento concreto:
```csharp
public interface IDocumentReport
{
    string ReportName   { get; }   // slug URL, ej. "anexo-grupos"
    string TemplateName { get; }   // nombre del .xlsx sin extensión
    Task<IReadOnlyDictionary<string, object>> GatherVariablesAsync(CancellationToken ct);
}
```
`GatherVariablesAsync` consulta la BD y devuelve un diccionario donde cada clave es el nombre de un Named Range en la plantilla y el valor es la lista de filas.

#### `IDocumentRenderer` (Application/Common/Interfaces/)
```csharp
public interface IDocumentRenderer
{
    byte[] Render(string templateName, IReadOnlyDictionary<string, object> variables);
}
```
Implementado por `DocumentRenderer` en Infrastructure, que:
1. Carga la plantilla desde un recurso embebido (`Infrastructure/Templates/*.xlsx`).
2. Inyecta cada variable con `template.AddVariable(name, value)`.
3. Llama a `template.Generate()` y devuelve el resultado como `byte[]`.

#### `GenerateDocumentQuery` (Application/Documents/Queries/)
Handler genérico que recibe el `ReportName` (del segmento URL), busca el `IDocumentReport` registrado con ese nombre, llama a `GatherVariablesAsync` y delega el renderizado a `IDocumentRenderer`.

#### Endpoint
```
GET /api/Documents/{reportName}
```
Requiere rol `Superuser` o `Jefe_de_Grupo_de_investigacion`. Devuelve el archivo con `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`.

---

### Documentos implementados

| ReportName | Plantilla | Hojas | Descripción |
|---|---|---|---|
| `anexo-grupos` | `AnexoGrupos.xlsx` | 1 | Grupos de Investigación (Anexo 10) |
| `anexo-proyectos` | `AnexoProyectos.xlsx` | 9 | Proyectos por tipo: PE, PAPN, PAPS, PAPT, PNE, PDL, PRCI, PNAP, Nuevas Aplicaciones |

### Documentos multi-hoja (AnexoProyectos)

Cada hoja tiene su propio Named Range con el nombre del tipo de proyecto. `ProyectosReport.GatherVariablesAsync` realiza **9 queries independientes** con `OfType<T>()` de EF Core (aprovecha TPT del ADR-0006) y un DTO específico por tipo que hereda los campos comunes de `ProyectoEnEjecucionRowDto`. Los PAP se subdividen en tres queries filtrando por `TipoPAP` (Nacional/Sectorial/Territorial).

### Plantillas físicas y TemplateGen

Las plantillas `.xlsx` viven en `Infrastructure/Templates/` y se embeben como `EmbeddedResource`. Para regenerarlas o crear nuevas existe el proyecto utilitario `TemplateGen/`:

```
TemplateGen/
  Program.cs              ← menú selector (interactivo o por argumento)
  Core/
    Base/
      ExcelTemplateBase.cs   ← base para libros multi-hoja
      SheetTemplateBase.cs   ← base para cada hoja individual
    Interfaces/
      IExcelTemplate.cs
      ISheetTemplate.cs
  Templates/
    AnexoGrupos.cs           ← genera AnexoGrupos.xlsx (una hoja)
    Proyectos/
      AnexoProyectosTemplate.cs   ← orquesta las 9 hojas
      Sheets/
        PESheet.cs, PAPNSheet.cs, ...  ← una clase por hoja
```

Uso:
```bash
dotnet run              # menú interactivo
dotnet run -- all       # genera todas las plantillas
dotnet run -- grupos    # solo AnexoGrupos.xlsx
```

### Registro en DI

```csharp
builder.Services.AddSingleton<IDocumentRenderer, DocumentRenderer>();
builder.Services.AddScoped<IDocumentReport, AnexoGruposReport>();
builder.Services.AddScoped<IDocumentReport, ProyectosReport>();
// Añadir una línea por cada nuevo documento
```

### Frontend

El botón **"Generar Anexo X"** aparece en las páginas con acceso restringido:
- `GruposDeInvestigacionPage` (Superuser) — junto al botón "+ Nuevo grupo".
- `MisGruposDeInvestigacionPage` (Jefe de Grupo) — en el header de la página.

El cliente hace un `fetch` con `credentials: 'include'`, recibe el blob y dispara la descarga mediante un `<a>` temporal creado en JavaScript.

---

### Dependencias y supresión de advertencias de seguridad

ClosedXML.Report 0.2.12 introduce transitivamente:
- `System.Security.Cryptography.Xml` — vulnerabilidad GHSA-37gx-xxp4-5rgx y GHSA-w3x6-4m5h-cxqf (XML signature). No aplica: el proyecto no firma ni verifica XML.
- `System.IO.Packaging` — vulnerabilidad GHSA-f32c-w444-8ppv y GHSA-qj66-m88j-hmgj (ZipPackage path traversal al leer archivos externos). No aplica: solo escribimos, nunca leemos archivos controlados por el usuario.

Se suprimen con `<NuGetAuditSuppress>` en `Directory.Build.props` y se fuerzan versiones más recientes en `Infrastructure.csproj`.

---

## Consecuencias

### Cómo añadir un nuevo documento

1. Diseñar `Infrastructure/Templates/MiDocumento.xlsx` con Named Ranges y expresiones `{{item.Xxx}}`.
2. Crear `Application/Documents/Reports/MiDocumentoReport.cs` implementando `IDocumentReport`.
3. Registrar: `builder.Services.AddScoped<IDocumentReport, MiDocumentoReport>();` en `Infrastructure/DependencyInjection.cs`.
4. Accesible en `GET /api/Documents/{ReportName}` automáticamente.

**No se modifica código existente.** Se cumple OCP.

### Trade-offs

- Las plantillas `.xlsx` embebidas viajan con el binario → para cambiar el diseño se necesita rebuild (aunque no reescritura de código).  
  _Alternativa futura_: cargar desde sistema de archivos o blob storage configurable.
- `DocumentRenderer` es `Singleton`: la carga de recursos embebidos es thread-safe; el workbook se crea por llamada.
