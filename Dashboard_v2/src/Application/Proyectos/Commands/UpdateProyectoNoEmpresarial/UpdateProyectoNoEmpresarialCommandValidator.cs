using Dashboard_v2.Application.Common.Interfaces;

namespace Dashboard_v2.Application.Proyectos.Commands.UpdateProyectoNoEmpresarial;

/// <summary>Validator de <see cref="UpdateProyectoNoEmpresarialCommand"/>. Hereda las reglas comunes de <see cref="ProyectoBaseValidator{T}"/>.</summary>
public class UpdateProyectoNoEmpresarialCommandValidator
    : ProyectoBaseValidator<UpdateProyectoNoEmpresarialCommand>
{
    public UpdateProyectoNoEmpresarialCommandValidator(IApplicationDbContext context)
        : base(context)
    {
        RuleFor(x => x.EntidadNoEmpresarial)
            .NotEmpty()
            .WithMessage("La entidad no empresarial es obligatoria.");
    }
}
