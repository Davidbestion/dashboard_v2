using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Proyectos.Commands.UpdateProyectoPNAP;

/// <summary>Validator de <see cref="UpdateProyectoPNAPCommand"/>. Hereda las reglas comunes de <see cref="ProyectoBaseValidator{T}"/>.</summary>
public class UpdateProyectoPNAPCommandValidator
    : ProyectoBaseValidator<UpdateProyectoPNAPCommand>
{
    public UpdateProyectoPNAPCommandValidator(IApplicationDbContext context)
        : base(context)
    {
        RuleFor(x => x.FinanciamientoUH)
            .NotEmpty()
            .WithMessage("El financiamiento UH es obligatorio.");
    }
}
