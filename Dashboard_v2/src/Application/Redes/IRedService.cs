using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Redes;

/// <summary>
/// Servicio de gestión de redes científicas: listados con filtro por rol/área,
/// coordinador, participantes y eventos vinculados.
/// </summary>
public interface IRedService
{
    /// <summary>Listado general de redes; aplica filtro por área cuando el usuario actual es Vicedecano.</summary>
    Task<List<RedDto>> GetRedesAsync(CancellationToken ct = default);

    /// <summary>Redes relevantes para el usuario actual: todas (Superuser), del área (Jefe_de_Redes) o propias (Profesor).</summary>
    Task<List<RedConCoordinadorDto>> GetMisRedesAsync(CancellationToken ct = default);

    Task<Result> SetCoordinadorAsync(string redId, string? coordinadorId, CancellationToken ct = default);

    Task<(bool Found, List<ParticipanteRedDto> Participantes)> GetParticipantesAsync(string redId, CancellationToken ct = default);

    Task<Result> AddParticipanteAsync(string redId, string authorId, CancellationToken ct = default);

    Task<Result> RemoveParticipanteAsync(string redId, string authorId, CancellationToken ct = default);

    Task<(Result Result, string? Id)> CreateRedAsync(CreateRedBody body, CancellationToken ct = default);

    Task<Result> UpdateRedAsync(string id, UpdateRedBody body, CancellationToken ct = default);

    Task<Result> DeleteRedAsync(string id, CancellationToken ct = default);

    Task<List<EventForRedDto>> GetEventsForRedAsync(string redId, CancellationToken ct = default);

    Task<Result> SetEventsForRedAsync(string redId, List<int> eventIds, CancellationToken ct = default);
}
