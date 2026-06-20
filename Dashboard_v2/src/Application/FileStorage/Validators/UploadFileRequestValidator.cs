using Dashboard_v2.Application.FileStorage.DTOs;

namespace Dashboard_v2.Application.FileStorage.Validators;

public sealed class UploadFileRequestValidator : AbstractValidator<UploadFileRequest>
{
    // Tipos MIME permitidos para subir al sistema
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
        "application/msword",                                                       // .doc
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",       // .xlsx
        "application/vnd.ms-excel",                                                 // .xls
    };

    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

    public UploadFileRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("El nombre del archivo es obligatorio.")
            .MaximumLength(260).WithMessage("El nombre del archivo no puede superar 260 caracteres.");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("El tipo de contenido es obligatorio.")
            .Must(ct => AllowedContentTypes.Contains(ct))
            .WithMessage($"Tipo de archivo no permitido. Tipos aceptados: PDF, Word (.doc/.docx), Excel (.xls/.xlsx).");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0).WithMessage("El archivo no puede estar vacío.")
            .LessThanOrEqualTo(MaxFileSizeBytes).WithMessage($"El archivo no puede superar {MaxFileSizeBytes / (1024 * 1024)} MB.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("La categoría es obligatoria.")
            .MaximumLength(50).WithMessage("La categoría no puede superar 50 caracteres.")
            .Matches(@"^[a-z0-9\-]+$").WithMessage("La categoría solo puede contener letras minúsculas, números y guiones.");
    }
}
