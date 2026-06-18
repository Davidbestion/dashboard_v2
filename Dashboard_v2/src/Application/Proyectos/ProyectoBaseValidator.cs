using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Proyectos;

/// <summary>
/// Validador base para todas las operaciones CRUD de escritura sobre proyectos.
/// Aplica las reglas comunes:
/// <list type="bullet">
/// <item>Título no puede estar vacío.</item>
/// <item>ClasificacionId debe existir en la base de datos.</item>
/// </list>
/// Los validadores concretos heredan de esta clase y añaden sus reglas específicas.
/// </summary>
public abstract class ProyectoBaseValidator<T> : AbstractValidator<T>
    where T : IProyectoUpsertRequest
{
    protected ProyectoBaseValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.Titulo)
            .NotEmpty()
            .WithMessage("El título es obligatorio.");

        RuleFor(x => x.ClasificacionId)
            .NotEmpty()
            .MustAsync(async (id, ct) => await context.Clasificaciones.AnyAsync(c => c.Id == id, ct))
            .WithMessage("La clasificación indicada no existe.");
    }
}
