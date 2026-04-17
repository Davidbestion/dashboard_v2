using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Proyectos.Commands.CreateProyectoColabInternacional;

/// <summary>Validator de <see cref="CreateProyectoColabInternacionalCommand"/>. Hereda las reglas comunes de <see cref="ProyectoBaseValidator{T}"/>.</summary>
public class CreateProyectoColabInternacionalCommandValidator
    : ProyectoBaseValidator<CreateProyectoColabInternacionalCommand>
{
    public CreateProyectoColabInternacionalCommandValidator(IApplicationDbContext context)
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
