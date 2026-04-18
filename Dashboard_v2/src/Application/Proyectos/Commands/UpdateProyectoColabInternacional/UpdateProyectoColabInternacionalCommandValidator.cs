using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Proyectos.Commands.UpdateProyectoColabInternacional;

/// <summary>Validator de <see cref="UpdateProyectoColabInternacionalCommand"/>. Hereda las reglas comunes de <see cref="ProyectoBaseValidator{T}"/>.</summary>
public class UpdateProyectoColabInternacionalCommandValidator
    : ProyectoBaseValidator<UpdateProyectoColabInternacionalCommand>
{
    public UpdateProyectoColabInternacionalCommandValidator(IApplicationDbContext context)
        : base(context)
    {
        RuleFor(x => x.FuenteFinanciacion)
            .NotEmpty()
            .WithMessage("La fuente de financiación es obligatoria.");

        RuleFor(x => x.TerminosReferencia)
            .NotEmpty()
            .WithMessage("Los términos de referencia son obligatorios.");
    }
}
