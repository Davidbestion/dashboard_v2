using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Proyectos.Commands.CreateProyectoPNAP;

/// <summary>Validator de <see cref="CreateProyectoPNAPCommand"/>. Hereda las reglas comunes de <see cref="ProyectoBaseValidator{T}"/>.</summary>
public class CreateProyectoPNAPCommandValidator
    : ProyectoBaseValidator<CreateProyectoPNAPCommand>
{
    public CreateProyectoPNAPCommandValidator(IApplicationDbContext context)
        : base(context)
    {
        RuleFor(x => x.FinanciamientoUH)
            .NotEmpty()
            .WithMessage("El financiamiento UH es obligatorio.");
    }
}
