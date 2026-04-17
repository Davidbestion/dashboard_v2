using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Proyectos.Commands.UpdateProyectoEnRevision;

/// <summary>Validator de <see cref="UpdateProyectoEnRevisionCommand"/>. Hereda las reglas comunes de <see cref="ProyectoBaseValidator{T}"/>.</summary>
public class UpdateProyectoEnRevisionCommandValidator
    : ProyectoBaseValidator<UpdateProyectoEnRevisionCommand>
{
    public UpdateProyectoEnRevisionCommandValidator(IApplicationDbContext context)
        : base(context) { }
}
