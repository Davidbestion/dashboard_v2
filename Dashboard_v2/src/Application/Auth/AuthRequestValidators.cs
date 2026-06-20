namespace Dashboard_v2.Application.Auth;

/// <summary>
/// Reglas de validación para el registro de usuarios.
/// </summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(v => v.UserName)
            .NotEmpty().WithMessage("El nombre de usuario es requerido.")
            .MinimumLength(3).WithMessage("El nombre de usuario debe tener al menos 3 caracteres.")
            .MaximumLength(100).WithMessage("El nombre de usuario no debe exceder 100 caracteres.");

        RuleFor(v => v.UserLastName1)
            .NotEmpty().WithMessage("El primer apellido es requerido.")
            .MaximumLength(256).WithMessage("El primer apellido no debe exceder 256 caracteres.");

        RuleFor(v => v.UserLastName2)
            .MaximumLength(256).When(v => v.UserLastName2 != null)
            .WithMessage("El segundo apellido no debe exceder 256 caracteres.");

        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("El email es requerido.")
            .EmailAddress().WithMessage("El email no es válido.")
            .MaximumLength(256).WithMessage("El email no debe exceder 256 caracteres.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.")
            .MaximumLength(100).WithMessage("La contraseña no debe exceder 100 caracteres.");

        RuleFor(v => v.BirthDate)
            .NotEmpty().WithMessage("La fecha de nacimiento es requerida.")
            .LessThan(DateTime.Today).WithMessage("La fecha de nacimiento debe ser anterior a hoy.");
    }
}

/// <summary>
/// Reglas de validación para el inicio de sesión.
/// </summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("El email es requerido.")
            .EmailAddress().WithMessage("El email no es válido.");

        RuleFor(v => v.Password)
            .NotEmpty().WithMessage("La contraseña es requerida.");
    }
}
