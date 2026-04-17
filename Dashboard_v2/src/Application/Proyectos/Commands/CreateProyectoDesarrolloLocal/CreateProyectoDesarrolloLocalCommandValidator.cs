using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Proyectos.Commands.CreateProyectoDesarrolloLocal;

/// <summary>Validator de <see cref="CreateProyectoDesarrolloLocalCommand"/>. Hereda las reglas comunes de <see cref="ProyectoBaseValidator{T}"/>.</summary>
public class CreateProyectoDesarrolloLocalCommandValidator
    : ProyectoBaseValidator<CreateProyectoDesarrolloLocalCommand>
{
    public CreateProyectoDesarrolloLocalCommandValidator(IApplicationDbContext context)
        : base(context)
    {
        RuleFor(x => x.Municipio)
            .NotEmpty()
            .WithMessage("El municipio es obligatorio.");
    }
}
