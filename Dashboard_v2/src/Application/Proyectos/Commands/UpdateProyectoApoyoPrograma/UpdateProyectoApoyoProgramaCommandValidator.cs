using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Proyectos.Commands.UpdateProyectoApoyoPrograma;

/// <summary>Validator de <see cref="UpdateProyectoApoyoProgramaCommand"/>. Hereda las reglas comunes de <see cref="ProyectoBaseValidator{T}"/>.</summary>
public class UpdateProyectoApoyoProgramaCommandValidator
    : ProyectoBaseValidator<UpdateProyectoApoyoProgramaCommand>
{
    public UpdateProyectoApoyoProgramaCommandValidator(IApplicationDbContext context)
        : base(context)
    {
        RuleFor(x => x.NombrePrograma)
            .NotEmpty()
            .WithMessage("El nombre del programa es obligatorio.");
    }
}
