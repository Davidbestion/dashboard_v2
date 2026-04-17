using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Proyectos;

/// <summary>
/// Validator base para todos los commands de proyectos. Aplica las reglas comunes:
/// <list type="bullet">
/// <item>Título no puede estar vacío.</item>
/// <item>ClasificacionId debe existir en la base de datos.</item>
/// </list>
/// Los validators concretos heredan de esta clase y añaden sus reglas específicas.
/// </summary>
public abstract class ProyectoBaseValidator<T> : AbstractValidator<T>
    where T : IProyectoCommand
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
