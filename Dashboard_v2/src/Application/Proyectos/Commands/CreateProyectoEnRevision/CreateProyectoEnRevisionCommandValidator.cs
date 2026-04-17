using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Proyectos.Commands.CreateProyectoEnRevision;

/// <summary>Validator de <see cref="CreateProyectoEnRevisionCommand"/>. Hereda las reglas comunes de <see cref="ProyectoBaseValidator{T}"/>.</summary>
public class CreateProyectoEnRevisionCommandValidator
    : ProyectoBaseValidator<CreateProyectoEnRevisionCommand>
{
    public CreateProyectoEnRevisionCommandValidator(IApplicationDbContext context)
        : base(context) { }
}
