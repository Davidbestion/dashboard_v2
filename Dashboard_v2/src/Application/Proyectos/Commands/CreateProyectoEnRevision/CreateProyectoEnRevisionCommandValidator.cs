using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Constants;

namespace Dashboard_v2.Application.Proyectos.Commands.CreateProyectoEnRevision;

/// <summary>Validator de <see cref="CreateProyectoEnRevisionCommand"/>. Hereda las reglas comunes de <see cref="ProyectoBaseValidator{T}"/>.</summary>
public class CreateProyectoEnRevisionCommandValidator
    : ProyectoBaseValidator<CreateProyectoEnRevisionCommand>
{
    public CreateProyectoEnRevisionCommandValidator(IApplicationDbContext context)
        : base(context)
    {
        RuleFor(x => x.Tipo)
            .NotEmpty().WithMessage("El tipo de proyecto es obligatorio.")
            .Must(t => TiposProyectoEjecucion.Validos.Contains(t))
            .WithMessage($"El tipo debe ser uno de: {string.Join(", ", TiposProyectoEjecucion.Validos.Order())}.");
    }
}
