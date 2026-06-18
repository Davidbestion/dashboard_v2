namespace Dashboard_v2.Application.Redes;

public record RedDto(string Id, string Nombre, int? CountryId, string? CountryName, int CantidadProfesores, int Tipo);

public record RedConCoordinadorDto(
    string Id,
    string Nombre,
    int Tipo,
    string? CountryName,
    int CantidadProfesores,
    string? CoordinadorId,
    string? CoordinadorNombre,
    string? CoordinadorEmail,
    List<ParticipanteRedDto> Participantes);

public record ParticipanteRedDto(string AuthorId, string AuthorNombre);

public record EventForRedDto(int Id, string Name, bool Assigned);

public record SetCoordinadorBody(string? CoordinadorId);

public record CreateRedBody(string Nombre, int CountryId, int CantidadProfesores, int Tipo);

public record UpdateRedBody(string Nombre, int CountryId, int CantidadProfesores, int Tipo);

public record SetEventsBody(List<int> EventIds);
