namespace Dashboard_v2.Application.Redes;

/// <summary>
/// Research network summary.
/// </summary>
public record RedDto(string Id, string Nombre, int? CountryId, string? CountryName, int CantidadProfesores, int Tipo);

/// <summary>
/// Full network details including coordinator, members, and events.
/// </summary>
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

/// <summary>Minimal author reference for a network participant.</summary>
public record ParticipanteRedDto(string AuthorId, string AuthorNombre);

/// <summary>Event entry with assignment flag for the network event assignment form.</summary>
public record EventForRedDto(int Id, string Name, bool Assigned);

public record SetCoordinadorBody(string? CoordinadorId);

public record CreateRedBody(string Nombre, int CountryId, int CantidadProfesores, int Tipo);

public record UpdateRedBody(string Nombre, int CountryId, int CantidadProfesores, int Tipo);

public record SetEventsBody(List<int> EventIds);
