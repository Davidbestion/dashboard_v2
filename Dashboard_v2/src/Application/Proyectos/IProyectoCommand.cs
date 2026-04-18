namespace Dashboard_v2.Application.Proyectos;

/// <summary>
/// Contrato compartido por todos los commands de creación y edición de proyectos.
/// Permite que <see cref="ProyectoBaseValidator{T}"/> aplique las reglas comunes
/// (Título no vacío, ClasificacionId existente) sin repetirlas en cada validator concreto.
/// </summary>
public interface IProyectoCommand
{
    string Titulo { get; }
    string ClasificacionId { get; }
}
