namespace Dashboard_v2.Domain.Enums;

/// <summary>Scope of a research network (university, national, or international).</summary>
public enum TipoRed
{
    /// <summary>University-internal research network.</summary>
    Universitaria = 0,
    /// <summary>National research network spanning multiple institutions within the country.</summary>
    Nacional = 1,
    /// <summary>International research network with foreign member institutions.</summary>
    Internacional = 2,
}
