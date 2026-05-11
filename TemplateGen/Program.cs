// TemplateGen — Generador de plantillas Excel para Dashboard_v2
//
// Uso:
//   dotnet run                         → muestra el menú interactivo
//   dotnet run -- all                  → genera TODAS las plantillas
//   dotnet run -- grupos               → genera AnexoGrupos.xlsx
//   dotnet run -- grupos-estudiantiles → genera AnexoGruposEstudiantiles.xlsx
//   dotnet run -- publicaciones        → genera AnexoPublicaciones.xlsx
//   dotnet run -- eventos              → genera AnexoEventosCientificos.xlsx
//   dotnet run -- premios              → genera AnexoPremios.xlsx
//
// Para añadir una nueva plantilla:
//   1. Crea una clase que herede de ExcelTemplateBase
//   2. Define sus hojas con implementaciones de ISheetTemplate
//   3. Registra la entrada en el diccionario 'templates' de este archivo

using TemplateGen.Templates;

// var templates = new Dictionary<string, (string Descripcion, Action Generate)>(StringComparer.OrdinalIgnoreCase)
// {
//     ["grupos"] = ("Anexo 10 — Grupos de Investigación  →  AnexoGrupos.xlsx", AnexoGrupos.Generate),
//     // Próximas plantillas — descomenta o añade cuando las implementes:
//     // ["proyectos"] = ("Anexo X — Proyectos de Investigación  →  AnexoProyectos.xlsx", AnexoProyectos.Generate),
// };

var templates = new Dictionary<string, (string Descripcion, Action Generate)>(StringComparer.OrdinalIgnoreCase)
{
    ["grupos"] = ("Anexo 10 — Grupos de Investigación", () => new AnexoGruposTemplate().Generate()),
    ["grupos-estudiantiles"] = ("Anexo 9 — Grupos Científicos Estudiantiles", () => new AnexoGruposEstudiantilesTemplate().Generate()),
    ["publicaciones"] = ("Anexo 2 — Publicaciones", () => new AnexoPublicacionesTemplate().Generate()),
    ["premios"] = ("Anexo 5 — Premios", () => new AnexoPremiosTemplate().Generate()),
    ["eventos"] = ("Anexo 3 — Eventos y actividades científicas", () => new AnexoEventosTemplate().Generate()),
    ["proyectos"] = ("Anexo 4 — Proyectos de Investigación", () => new AnexoProyectosTemplate().Generate()),
    ["registros"] = ("Anexo 7 — Patentes, Registros, Normas y Productos", () => new AnexoRegistrosTemplate().Generate()),
    ["redes-nac-inter"] = ("Anexo 6 — Redes Nacionales e Internacionales", () => new AnexoRedesNacInterTemplate().Generate()),
    ["redes-universitaria"] = ("Anexo 6 — Plantilla Red Universitaria (una por red)", () => new AnexoRedUniversitariaTemplate().Generate()),
};

// ─── Modo línea de comandos ────────────────────────────────────────────────
if (args.Length > 0)
{
    var key = args[0];

    if (key.Equals("all", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Generando todas las plantillas...\n");
        foreach (var (k, (desc, generate)) in templates)
        {
            Console.Write($"  [{k}] {desc} ... ");
            generate();
        }
        Console.WriteLine("\nListo.");
        return;
    }

    if (templates.TryGetValue(key, out var t))
    {
        Console.WriteLine($"Generando [{key}]...");
        t.Generate();
        Console.WriteLine("Listo.");
        return;
    }

    Console.WriteLine($"Plantilla '{key}' no encontrada. Claves disponibles: {string.Join(", ", templates.Keys)}");
    return;
}

// ─── Modo interactivo ──────────────────────────────────────────────────────
while (true)
{
    Console.WriteLine();
    Console.WriteLine("╔══════════════════════════════════════════════════╗");
    Console.WriteLine("║         TemplateGen — Plantillas Excel           ║");
    Console.WriteLine("╠══════════════════════════════════════════════════╣");

    int idx = 1;
    var keys = new List<string>();
    foreach (var (k, (desc, _)) in templates)
    {
        Console.WriteLine($"║  {idx,2}. [{k,-12}] {desc,-34}║");
        keys.Add(k);
        idx++;
    }

    Console.WriteLine("╠══════════════════════════════════════════════════╣");
    Console.WriteLine("║   0. Generar TODAS                               ║");
    Console.WriteLine("║   q. Salir                                       ║");
    Console.WriteLine("╚══════════════════════════════════════════════════╝");
    Console.Write("\nElige una opcion: ");

    var input = Console.ReadLine()?.Trim() ?? "";

    if (input.Equals("q", StringComparison.OrdinalIgnoreCase))
        break;

    if (input == "0")
    {
        Console.WriteLine("\nGenerando todas las plantillas...");
        foreach (var (_, (_, generate)) in templates)
            generate();
        Console.WriteLine("Listo.");
        continue;
    }

    if (int.TryParse(input, out int choice) && choice >= 1 && choice <= keys.Count)
    {
        var key = keys[choice - 1];
        Console.WriteLine($"\nGenerando [{key}]...");
        templates[key].Generate();
        Console.WriteLine("Listo.");
        continue;
    }

    // También acepta escribir la clave directamente (ej. "grupos")
    if (templates.TryGetValue(input, out var byName))
    {
        Console.WriteLine($"\nGenerando [{input}]...");
        byName.Generate();
        Console.WriteLine("Listo.");
        continue;
    }

    Console.WriteLine("Opción no válida, intenta de nuevo.");
}
