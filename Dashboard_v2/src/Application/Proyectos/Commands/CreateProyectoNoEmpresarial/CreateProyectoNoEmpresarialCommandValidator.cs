using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Proyectos.Commands.CreateProyectoNoEmpresarial;

/// <summary>Validator de <see cref="CreateProyectoNoEmpresarialCommand"/>. Hereda las reglas comunes de <see cref="ProyectoBaseValidator{T}"/>.</summary>
public class CreateProyectoNoEmpresarialCommandValidator
    : ProyectoBaseValidator<CreateProyectoNoEmpresarialCommand>
{
    public CreateProyectoNoEmpresarialCommandValidator(IApplicationDbContext context)
        : base(context)
    {
        RuleFor(x => x.EntidadNoEmpresarial)
            .NotEmpty()
            .WithMessage("La entidad no empresarial es obligatoria.");
    }
}
