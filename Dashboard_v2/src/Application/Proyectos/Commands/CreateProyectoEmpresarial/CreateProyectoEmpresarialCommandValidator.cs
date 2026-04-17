using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Proyectos.Commands.CreateProyectoEmpresarial;

/// <summary>Validator de <see cref="CreateProyectoEmpresarialCommand"/>. Hereda las reglas comunes de <see cref="ProyectoBaseValidator{T}"/>.</summary>
public class CreateProyectoEmpresarialCommandValidator
    : ProyectoBaseValidator<CreateProyectoEmpresarialCommand>
{
    public CreateProyectoEmpresarialCommandValidator(IApplicationDbContext context)
        : base(context)
    {
        RuleFor(x => x.Empresa)
            .NotEmpty()
            .WithMessage("El nombre de la empresa es obligatorio.");
    }
}
