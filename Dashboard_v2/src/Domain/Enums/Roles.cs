namespace Dashboard_v2.Domain.Enums;

/// <summary>User roles in the research management system, from most restrictive (Profesor) to most permissive (Superuser).</summary>
public enum Roles
{
    /// <summary>No role assigned.</summary>
    None = 0,
    /// <summary>Base faculty role with read access.</summary>
    Profesor = 1,
    /// <summary>Vice-dean; views institutional dashboards.</summary>
    Vicedecano_de_investigacion = 2,
    /// <summary>Project leader; can manage their own projects.</summary>
    Jefe_de_Proyecto = 3,
    /// <summary>Research group leader; can manage their group and its members.</summary>
    Jefe_de_Grupo_de_investigacion = 4,
    /// <summary>Macro-project leader; oversees multiple related projects.</summary>
    Jefe_de_Macroproyecto = 5,
    /// <summary>Research network coordinator; manages network membership and events.</summary>
    Jefe_de_Redes = 6,
    /// <summary>System administrator with full access.</summary>
    Superuser = 7,
}
