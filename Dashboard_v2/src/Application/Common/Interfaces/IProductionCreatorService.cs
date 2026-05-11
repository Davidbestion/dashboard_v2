namespace Dashboard_v2.Application.Common.Interfaces;

/// <summary>
/// Encapsula la lógica de resolución y adición de co-creadores adicionales
/// a las colecciones de entidades de producción (Norma, Registro, Patente, ProductoComercializado).
/// </summary>
public interface IProductionCreatorService
{
    /// <summary>
    /// Resuelve y agrega co-creadores adicionales a <paramref name="creadores"/> sin duplicar
    /// al creador actual ni entradas existentes. Admite tres vías de resolución: por
    /// <c>AuthorId</c>, por nombre de autor, y por <c>UserId</c>.
    /// </summary>
    Task AddAdditionalCreatorsAsync<TJoin>(
        ICollection<TJoin> creadores,
        string currentAuthorId,
        Func<string, TJoin> joinFactory,
        Func<TJoin, string> getAuthorId,
        IEnumerable<string>? additionalAuthorIds,
        IEnumerable<string>? additionalAuthorNames,
        IEnumerable<string>? additionalUserIds,
        CancellationToken cancellationToken = default)
        where TJoin : class;
}
