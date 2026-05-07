using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Constants;
using Dashboard_v2.Domain.Entities;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Proyectos;

/// <summary>
/// Implementa la lógica CRUD de proyectos y mantiene el detalle por subtipo dentro
/// de una única capa de servicio coherente con el resto de la aplicación.
/// </summary>
public sealed class ProyectoService : IProyectoService
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly IRequestValidationService _validationService;

    public ProyectoService(
        IApplicationDbContext context,
        IUser currentUser,
        IRequestValidationService validationService)
    {
        _context = context;
        _currentUser = currentUser;
        _validationService = validationService;
    }

    public async Task<List<ProyectoResumenDto>> GetAllAsync(CancellationToken ct = default)
    {
        var ownerFilter = ProyectoHelper.GetOwnerFilter(_currentUser);

        var enRevision = await _context.Proyectos.OfType<ProyectoEnRevision>()
            .Where(p => ownerFilter == null || p.JefeId == ownerFilter)
            .Select(p => new ProyectoResumenDto
            {
                Id = p.Id,
                Titulo = p.Titulo,
                JefeId = p.JefeId,
                Jefe = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1 + (p.JefeUsuario.UserLastName2 != null ? " " + p.JefeUsuario.UserLastName2 : ""),
                CorreoJefe = p.JefeUsuario.Email,
                NumeroMiembros = p.NumeroMiembros,
                ClasificacionId = p.ClasificacionId,
                ClasificacionNombre = p.Clasificacion.Nombre,
                AreaId = p.AreaId,
                AreaNombre = p.Area.Nombre,
                Tipo = "en-revision",
                Situacion = p.Situacion,
                PublicacionesDerivadas = p.PublicacionesDerivadas.Select(pub => pub.UrlDoi ?? pub.Title).ToList()
            })
            .ToListAsync(ct);

        var empresariales = await QueryEjecucionAsync(_context.Proyectos.OfType<ProyectoEmpresarial>(), ownerFilter, "empresariales", ct);
        var apoyoPrograma = await QueryEjecucionAsync(_context.Proyectos.OfType<ProyectoApoyoPrograma>(), ownerFilter, "apoyo-programa", ct);
        var desarrolloLocal = await QueryEjecucionAsync(_context.Proyectos.OfType<ProyectoDesarrolloLocal>(), ownerFilter, "desarrollo-local", ct);
        var noEmpresariales = await QueryEjecucionAsync(_context.Proyectos.OfType<ProyectoNoEmpresarial>(), ownerFilter, "no-empresariales", ct);
        var colabInternacional = await QueryEjecucionAsync(_context.Proyectos.OfType<ProyectoColabInternacional>(), ownerFilter, "colaboracion-internacional", ct);
        var pnap = await QueryEjecucionAsync(_context.Proyectos.OfType<ProyectoPNAP>(), ownerFilter, "pnap", ct);

        return enRevision
            .Concat(empresariales)
            .Concat(apoyoPrograma)
            .Concat(desarrolloLocal)
            .Concat(noEmpresariales)
            .Concat(colabInternacional)
            .Concat(pnap)
            .OrderBy(p => p.Titulo)
            .ToList();
    }

    public Task<IReadOnlyList<string>> GetTiposEjecucionAsync(CancellationToken ct = default)
    {
        IReadOnlyList<string> tipos = TiposProyectoEjecucion.Validos
            .Order()
            .ToList();
        return Task.FromResult(tipos);
    }

    public Task<List<ProyectoCatalogoDto>> GetCatalogoAsync(CancellationToken ct = default)
    {
        return _context.Proyectos
            .AsNoTracking()
            .OrderBy(p => p.Titulo)
            .Select(p => new ProyectoCatalogoDto(p.Id, p.Titulo))
            .ToListAsync(ct);
    }

    public Task<List<ProyectoPublicacionDto>> GetPublicacionesDelProyectoAsync(string proyectoId, CancellationToken ct = default)
    {
        return _context.Publications
            .AsNoTracking()
            .Where(p => p.ProyectoId == proyectoId)
            .OrderBy(p => p.Title)
            .Select(p => new ProyectoPublicacionDto
            {
                Id = p.Id,
                Title = p.Title,
                UrlDoi = p.UrlDoi
            })
            .ToListAsync(ct);
    }

    public Task<List<ProyectoPublicacionDto>> GetPublicacionesDisponiblesAsync(CancellationToken ct = default)
    {
        return _context.Publications
            .AsNoTracking()
            .Where(p => p.ProyectoId == null)
            .OrderBy(p => p.Title)
            .Select(p => new ProyectoPublicacionDto
            {
                Id = p.Id,
                Title = p.Title,
                UrlDoi = p.UrlDoi
            })
            .ToListAsync(ct);
    }

    public async Task<Result> LinkPublicacionAsync(string proyectoId, string publicationId, CancellationToken ct = default)
    {
        var publication = await _context.Publications.FirstOrDefaultAsync(p => p.Id == publicationId, ct);
        if (publication is null)
        {
            return Result.Failure(["Publicación no encontrada."]);
        }

        if (publication.ProyectoId is not null && publication.ProyectoId != proyectoId)
        {
            return Result.Failure(["Esta publicación ya está vinculada a otro proyecto."]);
        }

        publication.ProyectoId = proyectoId;
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> UnlinkPublicacionAsync(string proyectoId, string publicationId, CancellationToken ct = default)
    {
        var publication = await _context.Publications
            .FirstOrDefaultAsync(p => p.Id == publicationId && p.ProyectoId == proyectoId, ct);

        if (publication is null)
        {
            return Result.Failure(["Publicación no encontrada."]);
        }

        publication.ProyectoId = null;
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var proyecto = await _context.Proyectos.FindAsync([id], ct);
        if (proyecto is null)
        {
            return Result.Failure(["Proyecto no encontrado."]);
        }

        var ownerFilter = ProyectoHelper.GetOwnerFilter(_currentUser);
        if (ownerFilter is not null && proyecto.JefeId != ownerFilter)
        {
            return Result.Failure(["No tiene permiso para eliminar este proyecto."]);
        }

        _context.Proyectos.Remove(proyecto);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public Task<ProyectoEnRevisionDto?> GetEnRevisionByIdAsync(string id, CancellationToken ct = default)
        => GetProyectoAsync<ProyectoEnRevision, ProyectoEnRevisionDto>(id, MapEnRevisionDto, ct);

    public Task<(Result Result, string? Id)> CreateEnRevisionAsync(ProyectoEnRevisionUpsertRequest request, CancellationToken ct = default)
        => CreateAsync<ProyectoEnRevision, ProyectoEnRevisionUpsertRequest>(request, (proyecto, body) =>
        {
            proyecto.Situacion = body.Situacion?.Trim() ?? string.Empty;
            proyecto.Tipo = body.Tipo?.Trim() ?? string.Empty;
        }, ct);

    public Task<Result> UpdateEnRevisionAsync(string id, ProyectoEnRevisionUpsertRequest request, CancellationToken ct = default)
        => UpdateAsync<ProyectoEnRevision, ProyectoEnRevisionUpsertRequest>(id, request, (proyecto, body) =>
        {
            proyecto.Situacion = body.Situacion?.Trim() ?? string.Empty;
            proyecto.Tipo = body.Tipo?.Trim() ?? string.Empty;
        }, ct);

    public Task<ProyectoEmpresarialDto?> GetEmpresarialByIdAsync(string id, CancellationToken ct = default)
        => GetProyectoAsync<ProyectoEmpresarial, ProyectoEmpresarialDto>(id, MapEmpresarialDto, ct);

    public Task<(Result Result, string? Id)> CreateEmpresarialAsync(ProyectoEmpresarialUpsertRequest request, CancellationToken ct = default)
        => CreateEjecucionAsync<ProyectoEmpresarial, ProyectoEmpresarialUpsertRequest>(request, (proyecto, body) =>
        {
            proyecto.Empresa = body.Empresa?.Trim() ?? string.Empty;
        }, ct);

    public Task<Result> UpdateEmpresarialAsync(string id, ProyectoEmpresarialUpsertRequest request, CancellationToken ct = default)
        => UpdateEjecucionAsync<ProyectoEmpresarial, ProyectoEmpresarialUpsertRequest>(id, request, (proyecto, body) =>
        {
            proyecto.Empresa = body.Empresa?.Trim() ?? string.Empty;
        }, ct);

    public Task<ProyectoApoyoProgramaDto?> GetApoyoProgramaByIdAsync(string id, CancellationToken ct = default)
        => GetProyectoAsync<ProyectoApoyoPrograma, ProyectoApoyoProgramaDto>(id, MapApoyoProgramaDto, ct);

    public Task<(Result Result, string? Id)> CreateApoyoProgramaAsync(ProyectoApoyoProgramaUpsertRequest request, CancellationToken ct = default)
        => CreateEjecucionAsync<ProyectoApoyoPrograma, ProyectoApoyoProgramaUpsertRequest>(request, (proyecto, body) =>
        {
            proyecto.NombrePrograma = body.NombrePrograma?.Trim() ?? string.Empty;
            proyecto.TipoPAP = body.TipoPAP;
        }, ct);

    public Task<Result> UpdateApoyoProgramaAsync(string id, ProyectoApoyoProgramaUpsertRequest request, CancellationToken ct = default)
        => UpdateEjecucionAsync<ProyectoApoyoPrograma, ProyectoApoyoProgramaUpsertRequest>(id, request, (proyecto, body) =>
        {
            proyecto.NombrePrograma = body.NombrePrograma?.Trim() ?? string.Empty;
            proyecto.TipoPAP = body.TipoPAP;
        }, ct);

    public Task<ProyectoDesarrolloLocalDto?> GetDesarrolloLocalByIdAsync(string id, CancellationToken ct = default)
        => GetProyectoAsync<ProyectoDesarrolloLocal, ProyectoDesarrolloLocalDto>(id, MapDesarrolloLocalDto, ct);

    public Task<(Result Result, string? Id)> CreateDesarrolloLocalAsync(ProyectoDesarrolloLocalUpsertRequest request, CancellationToken ct = default)
        => CreateEjecucionAsync<ProyectoDesarrolloLocal, ProyectoDesarrolloLocalUpsertRequest>(request, (proyecto, body) =>
        {
            proyecto.Municipio = body.Municipio?.Trim() ?? string.Empty;
        }, ct, forceTributaDesarrolloLocal: true);

    public Task<Result> UpdateDesarrolloLocalAsync(string id, ProyectoDesarrolloLocalUpsertRequest request, CancellationToken ct = default)
        => UpdateEjecucionAsync<ProyectoDesarrolloLocal, ProyectoDesarrolloLocalUpsertRequest>(id, request, (proyecto, body) =>
        {
            proyecto.Municipio = body.Municipio?.Trim() ?? string.Empty;
        }, ct, forceTributaDesarrolloLocal: true);

    public Task<ProyectoNoEmpresarialDto?> GetNoEmpresarialByIdAsync(string id, CancellationToken ct = default)
        => GetProyectoAsync<ProyectoNoEmpresarial, ProyectoNoEmpresarialDto>(id, MapNoEmpresarialDto, ct);

    public Task<(Result Result, string? Id)> CreateNoEmpresarialAsync(ProyectoNoEmpresarialUpsertRequest request, CancellationToken ct = default)
        => CreateEjecucionAsync<ProyectoNoEmpresarial, ProyectoNoEmpresarialUpsertRequest>(request, (proyecto, body) =>
        {
            proyecto.EntidadNoEmpresarial = body.EntidadNoEmpresarial?.Trim() ?? string.Empty;
        }, ct);

    public Task<Result> UpdateNoEmpresarialAsync(string id, ProyectoNoEmpresarialUpsertRequest request, CancellationToken ct = default)
        => UpdateEjecucionAsync<ProyectoNoEmpresarial, ProyectoNoEmpresarialUpsertRequest>(id, request, (proyecto, body) =>
        {
            proyecto.EntidadNoEmpresarial = body.EntidadNoEmpresarial?.Trim() ?? string.Empty;
        }, ct);

    public Task<ProyectoColabInternacionalDto?> GetColabInternacionalByIdAsync(string id, CancellationToken ct = default)
        => GetProyectoAsync<ProyectoColabInternacional, ProyectoColabInternacionalDto>(id, MapColabInternacionalDto, ct);

    public Task<(Result Result, string? Id)> CreateColabInternacionalAsync(ProyectoColabInternacionalUpsertRequest request, CancellationToken ct = default)
        => CreateEjecucionAsync<ProyectoColabInternacional, ProyectoColabInternacionalUpsertRequest>(request, (proyecto, body) =>
        {
            proyecto.FuenteFinanciacion = body.FuenteFinanciacion?.Trim() ?? string.Empty;
            proyecto.TerminosReferencia = body.TerminosReferencia?.Trim() ?? string.Empty;
        }, ct);

    public Task<Result> UpdateColabInternacionalAsync(string id, ProyectoColabInternacionalUpsertRequest request, CancellationToken ct = default)
        => UpdateEjecucionAsync<ProyectoColabInternacional, ProyectoColabInternacionalUpsertRequest>(id, request, (proyecto, body) =>
        {
            proyecto.FuenteFinanciacion = body.FuenteFinanciacion?.Trim() ?? string.Empty;
            proyecto.TerminosReferencia = body.TerminosReferencia?.Trim() ?? string.Empty;
        }, ct);

    public Task<ProyectoPNAPDto?> GetPNAPByIdAsync(string id, CancellationToken ct = default)
        => GetProyectoAsync<ProyectoPNAP, ProyectoPNAPDto>(id, MapPnapDto, ct);

    public Task<(Result Result, string? Id)> CreatePNAPAsync(ProyectoPNAPUpsertRequest request, CancellationToken ct = default)
        => CreateEjecucionAsync<ProyectoPNAP, ProyectoPNAPUpsertRequest>(request, (proyecto, body) =>
        {
            proyecto.FinanciamientoUH = body.FinanciamientoUH?.Trim() ?? string.Empty;
        }, ct);

    public Task<Result> UpdatePNAPAsync(string id, ProyectoPNAPUpsertRequest request, CancellationToken ct = default)
        => UpdateEjecucionAsync<ProyectoPNAP, ProyectoPNAPUpsertRequest>(id, request, (proyecto, body) =>
        {
            proyecto.FinanciamientoUH = body.FinanciamientoUH?.Trim() ?? string.Empty;
        }, ct);

    private async Task<(Result Result, string? Id)> CreateAsync<TProyecto, TRequest>(
        TRequest request,
        Action<TProyecto, TRequest> applySpecific,
        CancellationToken ct)
        where TProyecto : Proyecto, new()
        where TRequest : IProyectoUpsertRequest
    {
        await _validationService.ValidateAndThrowAsync(request, ct);

        var (jefeValidation, jefeId) = await ResolveAndValidateJefeAsync(request.JefeId, ct);
        if (jefeValidation is not null)
        {
            return (jefeValidation, null);
        }

        var (areaError, areaId) = await ResolveAreaIdAsync(request.AreaId, ct);
        if (areaError is not null)
        {
            return (areaError, null);
        }

        var proyecto = new TProyecto();
        ApplyBase(proyecto, request, jefeId, areaId);
        applySpecific(proyecto, request);

        _context.Proyectos.Add(proyecto);
        await _context.SaveChangesAsync(ct);

        return (Result.Success(), proyecto.Id);
    }

    private async Task<(Result Result, string? Id)> CreateEjecucionAsync<TProyecto, TRequest>(
        TRequest request,
        Action<TProyecto, TRequest> applySpecific,
        CancellationToken ct,
        bool forceTributaDesarrolloLocal = false)
        where TProyecto : ProyectoEnEjecucion, new()
        where TRequest : IProyectoEnEjecucionUpsertRequest
    {
        return await CreateAsync<TProyecto, TRequest>(request, (proyecto, body) =>
        {
            ApplyEjecucion(proyecto, body, forceTributaDesarrolloLocal);
            applySpecific(proyecto, body);
        }, ct);
    }

    private async Task<Result> UpdateAsync<TProyecto, TRequest>(
        string id,
        TRequest request,
        Action<TProyecto, TRequest> applySpecific,
        CancellationToken ct)
        where TProyecto : Proyecto
        where TRequest : IProyectoUpsertRequest
    {
        await _validationService.ValidateAndThrowAsync(request, ct);

        var proyecto = await _context.Proyectos.OfType<TProyecto>()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (proyecto is null)
        {
            return Result.Failure(["Proyecto no encontrado."]);
        }

        var ownerFilter = ProyectoHelper.GetOwnerFilter(_currentUser);
        if (ownerFilter is not null && proyecto.JefeId != ownerFilter)
        {
            return Result.Failure(["No tiene permiso para modificar este proyecto."]);
        }

        var (jefeValidation, jefeId) = await ResolveAndValidateJefeAsync(request.JefeId, ct);
        if (jefeValidation is not null)
        {
            return jefeValidation;
        }

        var (areaError, areaId) = await ResolveAreaIdAsync(request.AreaId, ct);
        if (areaError is not null)
        {
            return areaError;
        }

        ApplyBase(proyecto, request, jefeId, areaId);
        applySpecific(proyecto, request);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    private Task<Result> UpdateEjecucionAsync<TProyecto, TRequest>(
        string id,
        TRequest request,
        Action<TProyecto, TRequest> applySpecific,
        CancellationToken ct,
        bool forceTributaDesarrolloLocal = false)
        where TProyecto : ProyectoEnEjecucion
        where TRequest : IProyectoEnEjecucionUpsertRequest
    {
        return UpdateAsync<TProyecto, TRequest>(id, request, (proyecto, body) =>
        {
            ApplyEjecucion(proyecto, body, forceTributaDesarrolloLocal);
            applySpecific(proyecto, body);
        }, ct);
    }

    private async Task<TDto?> GetProyectoAsync<TProyecto, TDto>(
        string id,
        Func<TProyecto, TDto> mapper,
        CancellationToken ct)
        where TProyecto : Proyecto
        where TDto : class
    {
        var ownerFilter = ProyectoHelper.GetOwnerFilter(_currentUser);

        var proyecto = await _context.Proyectos.OfType<TProyecto>()
            .Include(x => x.Clasificacion)
            .Include(x => x.Area)
            .Include(x => x.JefeUsuario)
            .Include(x => x.PublicacionesDerivadas)
            .FirstOrDefaultAsync(x => x.Id == id && (ownerFilter == null || x.JefeId == ownerFilter), ct);

        return proyecto is null ? null : mapper(proyecto);
    }

    private static Task<List<ProyectoResumenDto>> QueryEjecucionAsync<TProyecto>(
        IQueryable<TProyecto> source,
        string? ownerFilter,
        string tipo,
        CancellationToken ct)
        where TProyecto : ProyectoEnEjecucion
    {
        return source
            .Where(p => ownerFilter == null || p.JefeId == ownerFilter)
            .Select(p => new ProyectoResumenDto
            {
                Id = p.Id,
                Titulo = p.Titulo,
                JefeId = p.JefeId,
                Jefe = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1 + (p.JefeUsuario.UserLastName2 != null ? " " + p.JefeUsuario.UserLastName2 : ""),
                CorreoJefe = p.JefeUsuario.Email,
                NumeroMiembros = p.NumeroMiembros,
                ClasificacionId = p.ClasificacionId,
                ClasificacionNombre = p.Clasificacion.Nombre,
                AreaId = p.AreaId,
                AreaNombre = p.Area.Nombre,
                Tipo = tipo,
                CodigoProyecto = p.CodigoProyecto,
                EstadoDeEjecucion = p.EstadoDeEjecucion,
                PublicacionesDerivadas = p.PublicacionesDerivadas.Select(pub => pub.UrlDoi ?? pub.Title).ToList()
            })
            .ToListAsync(ct);
    }

    private async Task<(Result? ValidationResult, string JefeId)> ResolveAndValidateJefeAsync(string requestedJefeId, CancellationToken ct)
    {
        var jefeId = ProyectoHelper.ResolveJefeId(requestedJefeId, _currentUser);
        var validationResult = await ProyectoHelper.ValidateJefeAsync(_context, jefeId, ct);
        return (validationResult, jefeId);
    }

    private static void ApplyBase(Proyecto proyecto, IProyectoUpsertRequest request, string jefeId, string areaId)
    {
        ProyectoHelper.SetBase(
            proyecto,
            request.Titulo,
            jefeId,
            request.NumeroMiembros,
            request.CantidadMiembrosUH,
            request.CantidadEstudiantes,
            request.CantidadEstudiantesContratados,
            request.TributaFormacionDoctoral,
            request.ClasificacionId,
            areaId);
    }

    /// <summary>
    /// Resuelve el AreaId efectivo para la operación:
    /// - Si el usuario activo es Jefe_de_Proyecto, se ignora el AreaId del request y se usa
    ///   el que tenga asignado el propio usuario en la base de datos (seguridad: un jefe
    ///   nunca puede asignar sus proyectos a un área que no es la suya).
    /// - Si el usuario es Superuser u otro rol con permiso, se usa el AreaId del request.
    /// </summary>
    private async Task<(Result? Error, string AreaId)> ResolveAreaIdAsync(
        string requestAreaId, CancellationToken ct)
    {
        var isJefe = _currentUser.Roles?.Contains(nameof(RolesEnum.Jefe_de_Proyecto)) == true;
        if (!isJefe)
        {
            return (null, requestAreaId);
        }

        // Jefe_de_Proyecto: use their own assigned area, ignoring the request value.
        var userAreaId = await _context.Users
            .Where(u => u.Id == _currentUser.Id)
            .Select(u => u.AreaId)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(userAreaId))
        {
            return (Result.Failure(["El usuario no tiene un área asignada. Contacte a un administrador."]), string.Empty);
        }

        return (null, userAreaId);
    }

    private static void ApplyEjecucion(ProyectoEnEjecucion proyecto, IProyectoEnEjecucionUpsertRequest request, bool forceTributaDesarrolloLocal)
    {
        ProyectoHelper.SetEjecucion(
            proyecto,
            request.FechaInicio,
            request.FechaCierre,
            request.EstadoDeEjecucion,
            request.CodigoProyecto,
            request.EntidadEjecutoraPrincipal,
            request.EntidadEjecutoraParticipante,
            request.ContribucionSectoresEstrategicos,
            request.ContribucionEjesEstrategicos,
            forceTributaDesarrolloLocal || request.TributaDesarrolloLocal);
    }

    private static ProyectoEnRevisionDto MapEnRevisionDto(ProyectoEnRevision proyecto)
    {
        return new ProyectoEnRevisionDto
        {
            Id = proyecto.Id,
            Titulo = proyecto.Titulo,
            JefeId = proyecto.JefeId,
            Jefe = GetNombreCompleto(proyecto.JefeUsuario),
            CorreoJefe = proyecto.JefeUsuario.Email,
            NumeroMiembros = proyecto.NumeroMiembros,
            CantidadMiembrosUH = proyecto.CantidadMiembrosUH,
            CantidadEstudiantes = proyecto.CantidadEstudiantes,
            CantidadEstudiantesContratados = proyecto.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = proyecto.TributaFormacionDoctoral,
            ClasificacionId = proyecto.ClasificacionId,
            ClasificacionNombre = proyecto.Clasificacion.Nombre,
            AreaId = proyecto.AreaId,
            AreaNombre = proyecto.Area?.Nombre ?? string.Empty,
            Situacion = proyecto.Situacion,
            Tipo = proyecto.Tipo,
            PublicacionesDerivadas = GetPublicacionesDerivadas(proyecto)
        };
    }

    private static ProyectoEmpresarialDto MapEmpresarialDto(ProyectoEmpresarial proyecto)
    {
        return new ProyectoEmpresarialDto
        {
            Id = proyecto.Id,
            Titulo = proyecto.Titulo,
            JefeId = proyecto.JefeId,
            Jefe = GetNombreCompleto(proyecto.JefeUsuario),
            CorreoJefe = proyecto.JefeUsuario.Email,
            NumeroMiembros = proyecto.NumeroMiembros,
            CantidadMiembrosUH = proyecto.CantidadMiembrosUH,
            CantidadEstudiantes = proyecto.CantidadEstudiantes,
            CantidadEstudiantesContratados = proyecto.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = proyecto.TributaFormacionDoctoral,
            ClasificacionId = proyecto.ClasificacionId,
            ClasificacionNombre = proyecto.Clasificacion.Nombre,
            AreaId = proyecto.AreaId,
            AreaNombre = proyecto.Area?.Nombre ?? string.Empty,
            FechaInicio = proyecto.FechaInicio,
            FechaCierre = proyecto.FechaCierre,
            EstadoDeEjecucion = proyecto.EstadoDeEjecucion,
            CodigoProyecto = proyecto.CodigoProyecto,
            EntidadEjecutoraPrincipal = proyecto.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = proyecto.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = proyecto.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = proyecto.ContribucionEjesEstrategicos,
            TributaDesarrolloLocal = proyecto.TributaDesarrolloLocal,
            Empresa = proyecto.Empresa,
            PublicacionesDerivadas = GetPublicacionesDerivadas(proyecto)
        };
    }

    private static ProyectoApoyoProgramaDto MapApoyoProgramaDto(ProyectoApoyoPrograma proyecto)
    {
        return new ProyectoApoyoProgramaDto
        {
            Id = proyecto.Id,
            Titulo = proyecto.Titulo,
            JefeId = proyecto.JefeId,
            Jefe = GetNombreCompleto(proyecto.JefeUsuario),
            CorreoJefe = proyecto.JefeUsuario.Email,
            NumeroMiembros = proyecto.NumeroMiembros,
            CantidadMiembrosUH = proyecto.CantidadMiembrosUH,
            CantidadEstudiantes = proyecto.CantidadEstudiantes,
            CantidadEstudiantesContratados = proyecto.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = proyecto.TributaFormacionDoctoral,
            ClasificacionId = proyecto.ClasificacionId,
            ClasificacionNombre = proyecto.Clasificacion.Nombre,
            AreaId = proyecto.AreaId,
            AreaNombre = proyecto.Area?.Nombre ?? string.Empty,
            FechaInicio = proyecto.FechaInicio,
            FechaCierre = proyecto.FechaCierre,
            EstadoDeEjecucion = proyecto.EstadoDeEjecucion,
            CodigoProyecto = proyecto.CodigoProyecto,
            EntidadEjecutoraPrincipal = proyecto.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = proyecto.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = proyecto.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = proyecto.ContribucionEjesEstrategicos,
            TributaDesarrolloLocal = proyecto.TributaDesarrolloLocal,
            NombrePrograma = proyecto.NombrePrograma,
            TipoPAP = proyecto.TipoPAP,
            PublicacionesDerivadas = GetPublicacionesDerivadas(proyecto)
        };
    }

    private static ProyectoDesarrolloLocalDto MapDesarrolloLocalDto(ProyectoDesarrolloLocal proyecto)
    {
        return new ProyectoDesarrolloLocalDto
        {
            Id = proyecto.Id,
            Titulo = proyecto.Titulo,
            JefeId = proyecto.JefeId,
            Jefe = GetNombreCompleto(proyecto.JefeUsuario),
            CorreoJefe = proyecto.JefeUsuario.Email,
            NumeroMiembros = proyecto.NumeroMiembros,
            CantidadMiembrosUH = proyecto.CantidadMiembrosUH,
            CantidadEstudiantes = proyecto.CantidadEstudiantes,
            CantidadEstudiantesContratados = proyecto.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = proyecto.TributaFormacionDoctoral,
            ClasificacionId = proyecto.ClasificacionId,
            ClasificacionNombre = proyecto.Clasificacion.Nombre,
            AreaId = proyecto.AreaId,
            AreaNombre = proyecto.Area?.Nombre ?? string.Empty,
            FechaInicio = proyecto.FechaInicio,
            FechaCierre = proyecto.FechaCierre,
            EstadoDeEjecucion = proyecto.EstadoDeEjecucion,
            CodigoProyecto = proyecto.CodigoProyecto,
            EntidadEjecutoraPrincipal = proyecto.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = proyecto.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = proyecto.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = proyecto.ContribucionEjesEstrategicos,
            TributaDesarrolloLocal = proyecto.TributaDesarrolloLocal,
            Municipio = proyecto.Municipio,
            PublicacionesDerivadas = GetPublicacionesDerivadas(proyecto)
        };
    }

    private static ProyectoNoEmpresarialDto MapNoEmpresarialDto(ProyectoNoEmpresarial proyecto)
    {
        return new ProyectoNoEmpresarialDto
        {
            Id = proyecto.Id,
            Titulo = proyecto.Titulo,
            JefeId = proyecto.JefeId,
            Jefe = GetNombreCompleto(proyecto.JefeUsuario),
            CorreoJefe = proyecto.JefeUsuario.Email,
            NumeroMiembros = proyecto.NumeroMiembros,
            CantidadMiembrosUH = proyecto.CantidadMiembrosUH,
            CantidadEstudiantes = proyecto.CantidadEstudiantes,
            CantidadEstudiantesContratados = proyecto.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = proyecto.TributaFormacionDoctoral,
            ClasificacionId = proyecto.ClasificacionId,
            ClasificacionNombre = proyecto.Clasificacion.Nombre,
            AreaId = proyecto.AreaId,
            AreaNombre = proyecto.Area?.Nombre ?? string.Empty,
            FechaInicio = proyecto.FechaInicio,
            FechaCierre = proyecto.FechaCierre,
            EstadoDeEjecucion = proyecto.EstadoDeEjecucion,
            CodigoProyecto = proyecto.CodigoProyecto,
            EntidadEjecutoraPrincipal = proyecto.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = proyecto.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = proyecto.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = proyecto.ContribucionEjesEstrategicos,
            TributaDesarrolloLocal = proyecto.TributaDesarrolloLocal,
            EntidadNoEmpresarial = proyecto.EntidadNoEmpresarial,
            PublicacionesDerivadas = GetPublicacionesDerivadas(proyecto)
        };
    }

    private static ProyectoColabInternacionalDto MapColabInternacionalDto(ProyectoColabInternacional proyecto)
    {
        return new ProyectoColabInternacionalDto
        {
            Id = proyecto.Id,
            Titulo = proyecto.Titulo,
            JefeId = proyecto.JefeId,
            Jefe = GetNombreCompleto(proyecto.JefeUsuario),
            CorreoJefe = proyecto.JefeUsuario.Email,
            NumeroMiembros = proyecto.NumeroMiembros,
            CantidadMiembrosUH = proyecto.CantidadMiembrosUH,
            CantidadEstudiantes = proyecto.CantidadEstudiantes,
            CantidadEstudiantesContratados = proyecto.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = proyecto.TributaFormacionDoctoral,
            ClasificacionId = proyecto.ClasificacionId,
            ClasificacionNombre = proyecto.Clasificacion.Nombre,
            AreaId = proyecto.AreaId,
            AreaNombre = proyecto.Area?.Nombre ?? string.Empty,
            FechaInicio = proyecto.FechaInicio,
            FechaCierre = proyecto.FechaCierre,
            EstadoDeEjecucion = proyecto.EstadoDeEjecucion,
            CodigoProyecto = proyecto.CodigoProyecto,
            EntidadEjecutoraPrincipal = proyecto.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = proyecto.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = proyecto.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = proyecto.ContribucionEjesEstrategicos,
            TributaDesarrolloLocal = proyecto.TributaDesarrolloLocal,
            FuenteFinanciacion = proyecto.FuenteFinanciacion,
            TerminosReferencia = proyecto.TerminosReferencia,
            PublicacionesDerivadas = GetPublicacionesDerivadas(proyecto)
        };
    }

    private static ProyectoPNAPDto MapPnapDto(ProyectoPNAP proyecto)
    {
        return new ProyectoPNAPDto
        {
            Id = proyecto.Id,
            Titulo = proyecto.Titulo,
            JefeId = proyecto.JefeId,
            Jefe = GetNombreCompleto(proyecto.JefeUsuario),
            CorreoJefe = proyecto.JefeUsuario.Email,
            NumeroMiembros = proyecto.NumeroMiembros,
            CantidadMiembrosUH = proyecto.CantidadMiembrosUH,
            CantidadEstudiantes = proyecto.CantidadEstudiantes,
            CantidadEstudiantesContratados = proyecto.CantidadEstudiantesContratados,
            TributaFormacionDoctoral = proyecto.TributaFormacionDoctoral,
            ClasificacionId = proyecto.ClasificacionId,
            ClasificacionNombre = proyecto.Clasificacion.Nombre,
            AreaId = proyecto.AreaId,
            AreaNombre = proyecto.Area?.Nombre ?? string.Empty,
            FechaInicio = proyecto.FechaInicio,
            FechaCierre = proyecto.FechaCierre,
            EstadoDeEjecucion = proyecto.EstadoDeEjecucion,
            CodigoProyecto = proyecto.CodigoProyecto,
            EntidadEjecutoraPrincipal = proyecto.EntidadEjecutoraPrincipal,
            EntidadEjecutoraParticipante = proyecto.EntidadEjecutoraParticipante,
            ContribucionSectoresEstrategicos = proyecto.ContribucionSectoresEstrategicos,
            ContribucionEjesEstrategicos = proyecto.ContribucionEjesEstrategicos,
            TributaDesarrolloLocal = proyecto.TributaDesarrolloLocal,
            FinanciamientoUH = proyecto.FinanciamientoUH,
            PublicacionesDerivadas = GetPublicacionesDerivadas(proyecto)
        };
    }

    private static string GetNombreCompleto(User user)
        => user.UserName + " " + user.UserLastName1 + (user.UserLastName2 != null ? " " + user.UserLastName2 : "");

    private static List<string> GetPublicacionesDerivadas(Proyecto proyecto)
        => proyecto.PublicacionesDerivadas.Select(pub => pub.UrlDoi ?? pub.Title).ToList();
}
