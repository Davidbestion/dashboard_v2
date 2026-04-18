using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Proyectos.Commands.UpdateProyectoEmpresarial;

/// <summary>Validator de <see cref="UpdateProyectoEmpresarialCommand"/>. Hereda las reglas comunes de <see cref="ProyectoBaseValidator{T}"/>.</summary>
public class UpdateProyectoEmpresarialCommandValidator
    : ProyectoBaseValidator<UpdateProyectoEmpresarialCommand>
{
    public UpdateProyectoEmpresarialCommandValidator(IApplicationDbContext context)
        : base(context)
    {
        RuleFor(x => x.Empresa)
            .NotEmpty()
            .WithMessage("El nombre de la empresa es obligatorio.");
    }
}
