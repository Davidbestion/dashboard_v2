namespace Dashboard_v2.Domain.Entities;

public class AuthorRegistro
{
    public string AuthorId { get; set; } = default!;
    public Author Author { get; set; } = default!;

    public string RegistroId { get; set; } = default!;
    public Registro Registro { get; set; } = default!;
}
