using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Proyectos.Commands.UpdateProyectoDesarrolloLocal;

/// <summary>Validator de <see cref="UpdateProyectoDesarrolloLocalCommand"/>. Hereda las reglas comunes de <see cref="ProyectoBaseValidator{T}"/>.</summary>
public class UpdateProyectoDesarrolloLocalCommandValidator
    : ProyectoBaseValidator<UpdateProyectoDesarrolloLocalCommand>
{
    public UpdateProyectoDesarrolloLocalCommandValidator(IApplicationDbContext context)
        : base(context)
    {
        RuleFor(x => x.Municipio)
            .NotEmpty()
            .WithMessage("El municipio es obligatorio.");
    }
}
