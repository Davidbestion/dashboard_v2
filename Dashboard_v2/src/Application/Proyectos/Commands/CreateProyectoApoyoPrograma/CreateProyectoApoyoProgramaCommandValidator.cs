using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Proyectos.Commands.CreateProyectoApoyoPrograma;

/// <summary>Validator de <see cref="CreateProyectoApoyoProgramaCommand"/>. Hereda las reglas comunes de <see cref="ProyectoBaseValidator{T}"/>.</summary>
public class CreateProyectoApoyoProgramaCommandValidator
    : ProyectoBaseValidator<CreateProyectoApoyoProgramaCommand>
{
    public CreateProyectoApoyoProgramaCommandValidator(IApplicationDbContext context)
        : base(context)
    {
        RuleFor(x => x.NombrePrograma)
            .NotEmpty()
            .WithMessage("El nombre del programa es obligatorio.");
    }
}
