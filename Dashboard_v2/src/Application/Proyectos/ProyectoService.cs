using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Constants;
using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Application.Proyectos;

/// <summary>
/// Implementa la lógica CRUD de proyectos y mantiene el detalle por subtipo dentro
/// de una única capa de servicio coherente con el resto de la aplicación.
/// </summary>
public sealed class ProyectoService : IProyectoQueryService, IProyectoCommandService, IProyectoService
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

    public Task<List<ProyectoResumenDto>> GetAllAsync(CancellationToken ct = default)
        => QueryAllSubtiposAsync(ProyectoScope.ForOwner(ProyectoHelper.GetOwnerFilter(_currentUser)), ct);

    public Task<List<ProyectoResumenDto>> GetMisProyectosParticipacionAsync(CancellationToken ct = default)
        => QueryAllSubtiposAsync(ProyectoScope.ForParticipant(_currentUser.Id ?? string.Empty), ct);

    public async Task<List<ProyectoResumenDto>> GetAreaProyectosAsync(CancellationToken ct = default)
    {
        var areaId = await _context.GetUserAreaIdAsync(_currentUser.Id, ct) ?? string.Empty;
        return await QueryAllSubtiposAsync(ProyectoScope.ForArea(areaId), ct);
    }

    /// <summary>
    /// Aplica el filtro de alcance dado a los 7 subtipos de proyecto y combina los resultados
    /// en un único listado resumen ordenado por título.
    /// </summary>
    private async Task<List<ProyectoResumenDto>> QueryAllSubtiposAsync(ProyectoScope scope, CancellationToken ct)
    {
        var enRevision = await ProjectEnRevisionResumen(scope.Apply(_context.Proyectos.OfType<ProyectoEnRevision>()))
            .ToListAsync(ct);

        var empresariales = await QueryEjecucionAsync(scope.Apply(_context.Proyectos.OfType<ProyectoEmpresarial>()), null, "empresariales", ct);
        var apoyoPrograma = await QueryEjecucionAsync(scope.Apply(_context.Proyectos.OfType<ProyectoApoyoPrograma>()), null, "apoyo-programa", ct);
        var desarrolloLocal = await QueryEjecucionAsync(scope.Apply(_context.Proyectos.OfType<ProyectoDesarrolloLocal>()), null, "desarrollo-local", ct);
        var noEmpresariales = await QueryEjecucionAsync(scope.Apply(_context.Proyectos.OfType<ProyectoNoEmpresarial>()), null, "no-empresariales", ct);
        var colabInternacional = await QueryEjecucionAsync(scope.Apply(_context.Proyectos.OfType<ProyectoColabInternacional>()), null, "colaboracion-internacional", ct);
        var pnap = await QueryEjecucionAsync(scope.Apply(_context.Proyectos.OfType<ProyectoPNAP>()), null, "pnap", ct);

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

    private static IQueryable<ProyectoResumenDto> ProjectEnRevisionResumen(IQueryable<ProyectoEnRevision> source)
        => source.Select(p => new ProyectoResumenDto
        {
            Id = p.Id,
            Titulo = p.Titulo,
            JefeId = p.JefeId,
            Jefe = p.JefeUsuario.UserName + " " + p.JefeUsuario.UserLastName1 + (p.JefeUsuario.UserLastName2 != null ? " " + p.JefeUsuario.UserLastName2 : ""),
            CorreoJefe = p.JefeUsuario.Email,
            NumeroMiembros = p.NumeroMiembros,
            ClasificacionId = p.ClasificacionId,
            ClasificacionNombre = p.Clasificacion.Nombre,
            Participantes = p.Participantes.Select(u => new UserRefDto(u.Id,
                u.UserName + " " + u.UserLastName1 + (u.UserLastName2 != null ? " " + u.UserLastName2 : ""),
                u.Email)).ToList(),
            Tipo = "en-revision",
            Situaciones = p.Situaciones.Select(s => s.Nombre).ToList(),
            PublicacionesDerivadas = p.PublicacionesDerivadas.Select(pub => pub.UrlDoi ?? pub.Title).ToList()
        });

    /// <summary>
    /// Alcance de visibilidad aplicable a cualquier subtipo de <see cref="Proyecto"/>:
    /// por jefe (owner), por área académica, por participación, o sin restricción.
    /// </summary>
    private sealed class ProyectoScope
    {
        private readonly string? _ownerId;
        private readonly string? _areaId;
        private readonly string? _participantId;

        private ProyectoScope(string? ownerId, string? areaId, string? participantId)
        {
            _ownerId = ownerId;
            _areaId = areaId;
            _participantId = participantId;
        }

        /// <summary>Sin restricción si <paramref name="ownerId"/> es null (p.ej. Superuser).</summary>
        public static ProyectoScope ForOwner(string? ownerId) => new(ownerId, null, null);
        public static ProyectoScope ForArea(string areaId) => new(null, areaId, null);
        public static ProyectoScope ForParticipant(string participantId) => new(null, null, participantId);

        public IQueryable<TProyecto> Apply<TProyecto>(IQueryable<TProyecto> source) where TProyecto : Proyecto
        {
            if (_ownerId != null)
                return source.Where(p => p.JefeId == _ownerId);
            if (_areaId != null)
                return source.Where(p => p.JefeUsuario.AreaId == _areaId || p.Participantes.Any(u => u.AreaId == _areaId));
            if (_participantId != null)
                return source.Where(p => p.Participantes.Any(u => u.Id == _participantId));
            return source;
        }
    }

    public Task<IReadOnlyList<string>> GetTiposEjecucionAsync(CancellationToken ct = default)
    {
        IReadOnlyList<string> tipos = TiposProyectoEjecucion.Validos.Order().ToList();
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
            .Select(p => new ProyectoPublicacionDto { Id = p.Id, Title = p.Title, UrlDoi = p.UrlDoi })
            .ToListAsync(ct);
    }

    public Task<List<ProyectoPublicacionDto>> GetPublicacionesDisponiblesAsync(CancellationToken ct = default)
    {
        return _context.Publications
            .AsNoTracking()
            .Where(p => p.ProyectoId == null)
            .OrderBy(p => p.Title)
            .Select(p => new ProyectoPublicacionDto { Id = p.Id, Title = p.Title, UrlDoi = p.UrlDoi })
            .ToListAsync(ct);
    }

    public async Task<Result> LinkPublicacionAsync(string proyectoId, string publicationId, CancellationToken ct = default)
    {
        var publication = await _context.Publications.FirstOrDefaultAsync(p => p.Id == publicationId, ct);
        if (publication is null)
            return Result.Failure(["Publicación no encontrada."]);

        if (publication.ProyectoId is not null && publication.ProyectoId != proyectoId)
            return Result.Failure(["Esta publicación ya está vinculada a otro proyecto."]);

        publication.ProyectoId = proyectoId;
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> UnlinkPublicacionAsync(string proyectoId, string publicationId, CancellationToken ct = default)
    {
        var publication = await _context.Publications
            .FirstOrDefaultAsync(p => p.Id == publicationId && p.ProyectoId == proyectoId, ct);

        if (publication is null)
            return Result.Failure(["Publicación no encontrada."]);

        publication.ProyectoId = null;
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SetParticipantesAsync(string proyectoId, IList<string> participantesIds, CancellationToken ct = default)
    {
        var proyecto = await _context.Proyectos
            .Include(p => p.Participantes)
            .FirstOrDefaultAsync(p => p.Id == proyectoId, ct);

        if (proyecto is null)
            return Result.Failure(["Proyecto no encontrado."]);

        var ownerFilter = ProyectoHelper.GetOwnerFilter(_currentUser);
        if (ownerFilter is not null && proyecto.JefeId != ownerFilter)
            return Result.Failure(["No tiene permiso para modificar los participantes de este proyecto."]);

        var ids = participantesIds.Contains(proyecto.JefeId)
            ? participantesIds
            : [..participantesIds, proyecto.JefeId];
        proyecto.Participantes = await _context.Users
            .Where(u => ids.Contains(u.Id))
            .ToListAsync(ct);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var proyecto = await _context.Proyectos.FindAsync([id], ct);
        if (proyecto is null)
            return Result.Failure(["Proyecto no encontrado."]);

        var ownerFilter = ProyectoHelper.GetOwnerFilter(_currentUser);
        if (ownerFilter is not null && proyecto.JefeId != ownerFilter)
            return Result.Failure(["No tiene permiso para eliminar este proyecto."]);

        _context.Proyectos.Remove(proyecto);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── EnRevision ────────────────────────────────────────────────────────────

    public Task<ProyectoEnRevisionDto?> GetEnRevisionByIdAsync(string id, CancellationToken ct = default)
        => GetProyectoAsync<ProyectoEnRevision, ProyectoEnRevisionDto>(
            id, MapEnRevisionDto, ct,
            q => q.Include(p => p.Situaciones));

    public Task<(Result Result, string? Id)> CreateEnRevisionAsync(ProyectoEnRevisionUpsertRequest request, CancellationToken ct = default)
        => CreateAsync<ProyectoEnRevision, ProyectoEnRevisionUpsertRequest>(request, async (proyecto, body) =>
        {
            proyecto.Tipo = body.Tipo?.Trim() ?? string.Empty;
            proyecto.Situaciones = await _context.SituacionesProyecto
                .Where(s => body.SituacionesIds.Contains(s.Id))
                .ToListAsync(ct);
        }, ct);

    public Task<Result> UpdateEnRevisionAsync(string id, ProyectoEnRevisionUpsertRequest request, CancellationToken ct = default)
        => UpdateAsync<ProyectoEnRevision, ProyectoEnRevisionUpsertRequest>(id, request, async (proyecto, body) =>
        {
            proyecto.Tipo = body.Tipo?.Trim() ?? string.Empty;
            proyecto.Situaciones = await _context.SituacionesProyecto
                .Where(s => body.SituacionesIds.Contains(s.Id))
                .ToListAsync(ct);
        }, ct, q => q.Include(p => p.Situaciones));

    // ── Empresarial ───────────────────────────────────────────────────────────

    public Task<ProyectoEmpresarialDto?> GetEmpresarialByIdAsync(string id, CancellationToken ct = default)
        => GetProyectoAsync<ProyectoEmpresarial, ProyectoEmpresarialDto>(
            id, MapEmpresarialDto, ct,
            q => q
                .Include(p => p.EstadosDeEjecucion)
                .Include(p => p.EntidadesEjecutorasPrincipales)
                .Include(p => p.EntidadesEjecutorasParticipantes)
                .Include(p => p.SectoresEstrategicos)
                .Include(p => p.EjesEstrategicos)
                .Include(p => p.Empresas));

    public Task<(Result Result, string? Id)> CreateEmpresarialAsync(ProyectoEmpresarialUpsertRequest request, CancellationToken ct = default)
        => CreateEjecucionAsync<ProyectoEmpresarial, ProyectoEmpresarialUpsertRequest>(request, async (proyecto, body) =>
        {
            proyecto.Empresas = await _context.Institutions
                .Where(i => body.EmpresasIds.Contains(i.Id))
                .ToListAsync(ct);
        }, ct);

    public Task<Result> UpdateEmpresarialAsync(string id, ProyectoEmpresarialUpsertRequest request, CancellationToken ct = default)
        => UpdateEjecucionAsync<ProyectoEmpresarial, ProyectoEmpresarialUpsertRequest>(id, request, async (proyecto, body) =>
        {
            proyecto.Empresas = await _context.Institutions
                .Where(i => body.EmpresasIds.Contains(i.Id))
                .ToListAsync(ct);
        }, ct, additionalIncludes: q => q.Include(p => p.Empresas));

    // ── ApoyoPrograma ─────────────────────────────────────────────────────────

    public Task<ProyectoApoyoProgramaDto?> GetApoyoProgramaByIdAsync(string id, CancellationToken ct = default)
        => GetProyectoAsync<ProyectoApoyoPrograma, ProyectoApoyoProgramaDto>(
            id, MapApoyoProgramaDto, ct,
            q => q
                .Include(p => p.EstadosDeEjecucion)
                .Include(p => p.EntidadesEjecutorasPrincipales)
                .Include(p => p.EntidadesEjecutorasParticipantes)
                .Include(p => p.SectoresEstrategicos)
                .Include(p => p.EjesEstrategicos)
                .Include(p => p.Programas));

    public Task<(Result Result, string? Id)> CreateApoyoProgramaAsync(ProyectoApoyoProgramaUpsertRequest request, CancellationToken ct = default)
        => CreateEjecucionAsync<ProyectoApoyoPrograma, ProyectoApoyoProgramaUpsertRequest>(request, async (proyecto, body) =>
        {
            proyecto.TipoPAP = body.TipoPAP;
            proyecto.Programas = await _context.Programas
                .Where(p => body.ProgramasIds.Contains(p.Id))
                .ToListAsync(ct);
        }, ct);

    public Task<Result> UpdateApoyoProgramaAsync(string id, ProyectoApoyoProgramaUpsertRequest request, CancellationToken ct = default)
        => UpdateEjecucionAsync<ProyectoApoyoPrograma, ProyectoApoyoProgramaUpsertRequest>(id, request, async (proyecto, body) =>
        {
            proyecto.TipoPAP = body.TipoPAP;
            proyecto.Programas = await _context.Programas
                .Where(p => body.ProgramasIds.Contains(p.Id))
                .ToListAsync(ct);
        }, ct, additionalIncludes: q => q.Include(p => p.Programas));

    // ── DesarrolloLocal ───────────────────────────────────────────────────────

    public Task<ProyectoDesarrolloLocalDto?> GetDesarrolloLocalByIdAsync(string id, CancellationToken ct = default)
        => GetProyectoAsync<ProyectoDesarrolloLocal, ProyectoDesarrolloLocalDto>(
            id, MapDesarrolloLocalDto, ct,
            q => q
                .Include(p => p.EstadosDeEjecucion)
                .Include(p => p.EntidadesEjecutorasPrincipales)
                .Include(p => p.EntidadesEjecutorasParticipantes)
                .Include(p => p.SectoresEstrategicos)
                .Include(p => p.EjesEstrategicos)
                .Include(p => p.Municipio).ThenInclude(m => m.Provincia));

    public Task<(Result Result, string? Id)> CreateDesarrolloLocalAsync(ProyectoDesarrolloLocalUpsertRequest request, CancellationToken ct = default)
        => CreateEjecucionAsync<ProyectoDesarrolloLocal, ProyectoDesarrolloLocalUpsertRequest>(request, (proyecto, body) =>
        {
            proyecto.MunicipioId = body.MunicipioId;
            return Task.CompletedTask;
        }, ct, forceTributaDesarrolloLocal: true);

    public Task<Result> UpdateDesarrolloLocalAsync(string id, ProyectoDesarrolloLocalUpsertRequest request, CancellationToken ct = default)
        => UpdateEjecucionAsync<ProyectoDesarrolloLocal, ProyectoDesarrolloLocalUpsertRequest>(id, request, (proyecto, body) =>
        {
            proyecto.MunicipioId = body.MunicipioId;
            return Task.CompletedTask;
        }, ct, forceTributaDesarrolloLocal: true);

    // ── NoEmpresarial ─────────────────────────────────────────────────────────

    public Task<ProyectoNoEmpresarialDto?> GetNoEmpresarialByIdAsync(string id, CancellationToken ct = default)
        => GetProyectoAsync<ProyectoNoEmpresarial, ProyectoNoEmpresarialDto>(
            id, MapNoEmpresarialDto, ct,
            q => q
                .Include(p => p.EstadosDeEjecucion)
                .Include(p => p.EntidadesEjecutorasPrincipales)
                .Include(p => p.EntidadesEjecutorasParticipantes)
                .Include(p => p.SectoresEstrategicos)
                .Include(p => p.EjesEstrategicos)
                .Include(p => p.Entidades));

    public Task<(Result Result, string? Id)> CreateNoEmpresarialAsync(ProyectoNoEmpresarialUpsertRequest request, CancellationToken ct = default)
        => CreateEjecucionAsync<ProyectoNoEmpresarial, ProyectoNoEmpresarialUpsertRequest>(request, async (proyecto, body) =>
        {
            proyecto.Entidades = await _context.Institutions
                .Where(i => body.EntidadesIds.Contains(i.Id))
                .ToListAsync(ct);
        }, ct);

    public Task<Result> UpdateNoEmpresarialAsync(string id, ProyectoNoEmpresarialUpsertRequest request, CancellationToken ct = default)
        => UpdateEjecucionAsync<ProyectoNoEmpresarial, ProyectoNoEmpresarialUpsertRequest>(id, request, async (proyecto, body) =>
        {
            proyecto.Entidades = await _context.Institutions
                .Where(i => body.EntidadesIds.Contains(i.Id))
                .ToListAsync(ct);
        }, ct, additionalIncludes: q => q.Include(p => p.Entidades));

    // ── ColabInternacional ────────────────────────────────────────────────────

    public Task<ProyectoColabInternacionalDto?> GetColabInternacionalByIdAsync(string id, CancellationToken ct = default)
        => GetProyectoAsync<ProyectoColabInternacional, ProyectoColabInternacionalDto>(
            id, MapColabInternacionalDto, ct,
            q => q
                .Include(p => p.EstadosDeEjecucion)
                .Include(p => p.EntidadesEjecutorasPrincipales)
                .Include(p => p.EntidadesEjecutorasParticipantes)
                .Include(p => p.SectoresEstrategicos)
                .Include(p => p.EjesEstrategicos)
                .Include(p => p.FuentesFinanciacion));

    public Task<(Result Result, string? Id)> CreateColabInternacionalAsync(ProyectoColabInternacionalUpsertRequest request, CancellationToken ct = default)
        => CreateEjecucionAsync<ProyectoColabInternacional, ProyectoColabInternacionalUpsertRequest>(request, async (proyecto, body) =>
        {
            proyecto.TerminosReferencia = body.TerminosReferencia?.Trim() ?? string.Empty;
            proyecto.FuentesFinanciacion = await _context.FuentesFinanciacion
                .Where(f => body.FuentesFinanciacionIds.Contains(f.Id))
                .ToListAsync(ct);
        }, ct);

    public Task<Result> UpdateColabInternacionalAsync(string id, ProyectoColabInternacionalUpsertRequest request, CancellationToken ct = default)
        => UpdateEjecucionAsync<ProyectoColabInternacional, ProyectoColabInternacionalUpsertRequest>(id, request, async (proyecto, body) =>
        {
            proyecto.TerminosReferencia = body.TerminosReferencia?.Trim() ?? string.Empty;
            proyecto.FuentesFinanciacion = await _context.FuentesFinanciacion
                .Where(f => body.FuentesFinanciacionIds.Contains(f.Id))
                .ToListAsync(ct);
        }, ct, additionalIncludes: q => q.Include(p => p.FuentesFinanciacion));

    // ── PNAP ──────────────────────────────────────────────────────────────────

    public Task<ProyectoPNAPDto?> GetPNAPByIdAsync(string id, CancellationToken ct = default)
        => GetProyectoAsync<ProyectoPNAP, ProyectoPNAPDto>(
            id, MapPnapDto, ct,
            q => q
                .Include(p => p.EstadosDeEjecucion)
                .Include(p => p.EntidadesEjecutorasPrincipales)
                .Include(p => p.EntidadesEjecutorasParticipantes)
                .Include(p => p.SectoresEstrategicos)
                .Include(p => p.EjesEstrategicos)
                .Include(p => p.FuentesFinanciacion));

    public Task<(Result Result, string? Id)> CreatePNAPAsync(ProyectoPNAPUpsertRequest request, CancellationToken ct = default)
        => CreateEjecucionAsync<ProyectoPNAP, ProyectoPNAPUpsertRequest>(request, async (proyecto, body) =>
        {
            proyecto.FuentesFinanciacion = await _context.FuentesFinanciacion
                .Where(f => body.FuentesFinanciacionIds.Contains(f.Id))
                .ToListAsync(ct);
        }, ct);

    public Task<Result> UpdatePNAPAsync(string id, ProyectoPNAPUpsertRequest request, CancellationToken ct = default)
        => UpdateEjecucionAsync<ProyectoPNAP, ProyectoPNAPUpsertRequest>(id, request, async (proyecto, body) =>
        {
            proyecto.FuentesFinanciacion = await _context.FuentesFinanciacion
                .Where(f => body.FuentesFinanciacionIds.Contains(f.Id))
                .ToListAsync(ct);
        }, ct, additionalIncludes: q => q.Include(p => p.FuentesFinanciacion));

    // ── Patentes derivadas ────────────────────────────────────────────────────

    public async Task<List<ProyectoPatenteResumenDto>> GetPatentesDelProyectoAsync(string proyectoId, CancellationToken ct = default)
    {
        var ownerFilter = ProyectoHelper.GetOwnerFilter(_currentUser);
        if (ownerFilter is not null && !await _context.Proyectos.AnyAsync(p => p.Id == proyectoId && p.JefeId == ownerFilter, ct))
            return [];

        return await _context.ProyectoPatentes
            .Where(pp => pp.ProyectoId == proyectoId)
            .Select(pp => new ProyectoPatenteResumenDto(
                pp.PatenteId,
                pp.Patente.Titulo,
                pp.Patente.NumeroSolicitudConcesion,
                pp.Patente.EsNacional,
                pp.Patente.Creadores.OrderBy(c => c.Author.Name).Select(c => c.Author.Name).FirstOrDefault(),
                pp.Patente.Creadores.OrderBy(c => c.Author.Name).Select(c => c.Author.Name).ToList()))
            .ToListAsync(ct);
    }

    public async Task<Result> LinkPatenteAsync(string proyectoId, string patenteId, CancellationToken ct = default)
    {
        var proyecto = await _context.Proyectos.FirstOrDefaultAsync(p => p.Id == proyectoId, ct);
        if (proyecto is null)
            return Result.Failure(["Proyecto no encontrado."]);
        if (!await _context.Patentes.AnyAsync(p => p.Id == patenteId, ct))
            return Result.Failure(["Patente no encontrada."]);
        if (await _context.ProyectoPatentes.AnyAsync(pp => pp.ProyectoId == proyectoId && pp.PatenteId == patenteId, ct))
            return Result.Failure(["El vínculo ya existe."]);

        var ownerFilter = ProyectoHelper.GetOwnerFilter(_currentUser);
        if (ownerFilter is not null && proyecto.JefeId != ownerFilter)
            return Result.Failure(["No tiene permisos sobre este proyecto."]);

        _context.ProyectoPatentes.Add(new ProyectoPatente { ProyectoId = proyectoId, PatenteId = patenteId });
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> UnlinkPatenteAsync(string proyectoId, string patenteId, CancellationToken ct = default)
    {
        var proyecto = await _context.Proyectos.FirstOrDefaultAsync(p => p.Id == proyectoId, ct);
        if (proyecto is null)
            return Result.Failure(["Proyecto no encontrado."]);

        var ownerFilter = ProyectoHelper.GetOwnerFilter(_currentUser);
        if (ownerFilter is not null && proyecto.JefeId != ownerFilter)
            return Result.Failure(["No tiene permisos sobre este proyecto."]);

        var link = await _context.ProyectoPatentes
            .FirstOrDefaultAsync(pp => pp.ProyectoId == proyectoId && pp.PatenteId == patenteId, ct);
        if (link is null)
            return Result.Failure(["Vínculo no encontrado."]);

        _context.ProyectoPatentes.Remove(link);
        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ── Generic CRUD helpers ──────────────────────────────────────────────────

    private async Task<(Result Result, string? Id)> CreateAsync<TProyecto, TRequest>(
        TRequest request,
        Func<TProyecto, TRequest, Task> applySpecificAsync,
        CancellationToken ct)
        where TProyecto : Proyecto, new()
        where TRequest : IProyectoUpsertRequest
    {
        await _validationService.ValidateAndThrowAsync(request, ct);

        var (jefeValidation, jefeId) = await ResolveAndValidateJefeAsync(request.JefeId, ct);
        if (jefeValidation is not null)
            return (jefeValidation, null);

        var proyecto = new TProyecto();
        ApplyBase(proyecto, request, jefeId);
        proyecto.Participantes = await _context.Users
            .Where(u => request.ParticipantesIds.Contains(u.Id) || u.Id == jefeId)
            .ToListAsync(ct);
        await applySpecificAsync(proyecto, request);

        _context.Proyectos.Add(proyecto);
        await _context.SaveChangesAsync(ct);

        return (Result.Success(), proyecto.Id);
    }

    private Task<(Result Result, string? Id)> CreateEjecucionAsync<TProyecto, TRequest>(
        TRequest request,
        Func<TProyecto, TRequest, Task> applySpecificAsync,
        CancellationToken ct,
        bool forceTributaDesarrolloLocal = false)
        where TProyecto : ProyectoEnEjecucion, new()
        where TRequest : IProyectoEnEjecucionUpsertRequest
    {
        return CreateAsync<TProyecto, TRequest>(request, async (proyecto, body) =>
        {
            await ApplyEjecucionAsync(proyecto, body, forceTributaDesarrolloLocal, ct);
            await applySpecificAsync(proyecto, body);
        }, ct);
    }

    private async Task<Result> UpdateAsync<TProyecto, TRequest>(
        string id,
        TRequest request,
        Func<TProyecto, TRequest, Task> applySpecificAsync,
        CancellationToken ct,
        Func<IQueryable<TProyecto>, IQueryable<TProyecto>>? includeBuilder = null)
        where TProyecto : Proyecto
        where TRequest : IProyectoUpsertRequest
    {
        await _validationService.ValidateAndThrowAsync(request, ct);

        IQueryable<TProyecto> query = _context.Proyectos.OfType<TProyecto>()
            .Include(p => p.Participantes);
        if (includeBuilder is not null)
            query = includeBuilder(query);

        var proyecto = await query.FirstOrDefaultAsync(p => p.Id == id, ct);

        if (proyecto is null)
            return Result.Failure(["Proyecto no encontrado."]);

        var ownerFilter = ProyectoHelper.GetOwnerFilter(_currentUser);
        if (ownerFilter is not null && proyecto.JefeId != ownerFilter)
            return Result.Failure(["No tiene permiso para modificar este proyecto."]);

        var (jefeValidation, jefeId) = await ResolveAndValidateJefeAsync(request.JefeId, ct);
        if (jefeValidation is not null)
            return jefeValidation;

        ApplyBase(proyecto, request, jefeId);
        proyecto.Participantes = await _context.Users
            .Where(u => request.ParticipantesIds.Contains(u.Id) || u.Id == jefeId)
            .ToListAsync(ct);
        await applySpecificAsync(proyecto, request);

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    private Task<Result> UpdateEjecucionAsync<TProyecto, TRequest>(
        string id,
        TRequest request,
        Func<TProyecto, TRequest, Task> applySpecificAsync,
        CancellationToken ct,
        bool forceTributaDesarrolloLocal = false,
        Func<IQueryable<TProyecto>, IQueryable<TProyecto>>? additionalIncludes = null)
        where TProyecto : ProyectoEnEjecucion
        where TRequest : IProyectoEnEjecucionUpsertRequest
    {
        // Always include the M:N collections owned by ProyectoEnEjecucion so EF Core
        // can detect deletions when the collections are reassigned during update.
        Func<IQueryable<TProyecto>, IQueryable<TProyecto>> fullIncludes = q =>
        {
            IQueryable<TProyecto> withBase = q
                .Include(p => p.EstadosDeEjecucion)
                .Include(p => p.EntidadesEjecutorasPrincipales)
                .Include(p => p.EntidadesEjecutorasParticipantes)
                .Include(p => p.SectoresEstrategicos)
                .Include(p => p.EjesEstrategicos);
            return additionalIncludes is null ? withBase : additionalIncludes(withBase);
        };

        return UpdateAsync<TProyecto, TRequest>(id, request, async (proyecto, body) =>
        {
            await ApplyEjecucionAsync(proyecto, body, forceTributaDesarrolloLocal, ct);
            await applySpecificAsync(proyecto, body);
        }, ct, fullIncludes);
    }

    private async Task<TDto?> GetProyectoAsync<TProyecto, TDto>(
        string id,
        Func<TProyecto, TDto> mapper,
        CancellationToken ct,
        Func<IQueryable<TProyecto>, IQueryable<TProyecto>>? includeBuilder = null)
        where TProyecto : Proyecto
        where TDto : class
    {
        var ownerFilter = ProyectoHelper.GetOwnerFilter(_currentUser);

        IQueryable<TProyecto> query = _context.Proyectos.OfType<TProyecto>()
            .Include(x => x.Clasificacion)
            .Include(x => x.Participantes)
            .Include(x => x.JefeUsuario)
            .Include(x => x.PublicacionesDerivadas);

        if (includeBuilder is not null)
            query = includeBuilder(query);

        var proyecto = await query
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
                Participantes = p.Participantes.Select(u => new UserRefDto(u.Id,
                    u.UserName + " " + u.UserLastName1 + (u.UserLastName2 != null ? " " + u.UserLastName2 : ""),
                    u.Email)).ToList(),
                Tipo = tipo,
                CodigoProyecto = p.CodigoProyecto,
                EstadosDeEjecucion = p.EstadosDeEjecucion.Select(e => e.Nombre).ToList(),
                PublicacionesDerivadas = p.PublicacionesDerivadas.Select(pub => pub.UrlDoi ?? pub.Title).ToList()
            })
            .ToListAsync(ct);
    }

    // ── Async M:N application ─────────────────────────────────────────────────

    private async Task ApplyEjecucionAsync(
        ProyectoEnEjecucion pe,
        IProyectoEnEjecucionUpsertRequest request,
        bool forceTributaDesarrolloLocal,
        CancellationToken ct)
    {
        ProyectoHelper.SetEjecucion(pe, request.FechaInicio, request.FechaCierre,
            request.CodigoProyecto, forceTributaDesarrolloLocal || request.TributaDesarrolloLocal);

        pe.EstadosDeEjecucion = await _context.EstadosProyecto
            .Where(e => request.EstadosDeEjecucionIds.Contains(e.Id))
            .ToListAsync(ct);

        pe.EntidadesEjecutorasPrincipales = await _context.Institutions
            .Where(i => request.EntidadesEjecutorasPrincipalesIds.Contains(i.Id))
            .ToListAsync(ct);

        pe.EntidadesEjecutorasParticipantes = await _context.Institutions
            .Where(i => request.EntidadesEjecutorasParticipantesIds.Contains(i.Id))
            .ToListAsync(ct);

        pe.SectoresEstrategicos = await _context.SectoresEstrategicos
            .Where(s => request.SectoresEstrategicosIds.Contains(s.Id))
            .ToListAsync(ct);

        pe.EjesEstrategicos = await _context.EjesEstrategicos
            .Where(e => request.EjesEstrategicosIds.Contains(e.Id))
            .ToListAsync(ct);
    }

    // ── Resolution helpers ────────────────────────────────────────────────────

    private async Task<(Result? ValidationResult, string JefeId)> ResolveAndValidateJefeAsync(string requestedJefeId, CancellationToken ct)
    {
        var jefeId = ProyectoHelper.ResolveJefeId(requestedJefeId, _currentUser);
        var validationResult = await ProyectoHelper.ValidateJefeAsync(_context, jefeId, ct);
        return (validationResult, jefeId);
    }

    private static void ApplyBase(Proyecto proyecto, IProyectoUpsertRequest request, string jefeId)
    {
        ProyectoHelper.SetBase(
            proyecto, request.Titulo, jefeId, request.NumeroMiembros,
            request.CantidadMiembrosUH, request.CantidadEstudiantes,
            request.CantidadEstudiantesContratados, request.TributaFormacionDoctoral,
            request.ClasificacionId);
    }

    // ── Mappers ───────────────────────────────────────────────────────────────

    private static ProyectoEnRevisionDto MapEnRevisionDto(ProyectoEnRevision proyecto) => new()
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
        Participantes = MapParticipantes(proyecto.Participantes),
        Situaciones = proyecto.Situaciones.Select(s => new NomencladorDto(s.Id, s.Nombre)).ToList(),
        Tipo = proyecto.Tipo,
        PublicacionesDerivadas = GetPublicacionesDerivadas(proyecto)
    };

    private static ProyectoEmpresarialDto MapEmpresarialDto(ProyectoEmpresarial proyecto) => new()
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
        Participantes = MapParticipantes(proyecto.Participantes),
        FechaInicio = proyecto.FechaInicio,
        FechaCierre = proyecto.FechaCierre,
        CodigoProyecto = proyecto.CodigoProyecto,
        TributaDesarrolloLocal = proyecto.TributaDesarrolloLocal,
        EstadosDeEjecucion = MapNomencladores(proyecto.EstadosDeEjecucion),
        EntidadesEjecutorasPrincipales = MapInstitutions(proyecto.EntidadesEjecutorasPrincipales),
        EntidadesEjecutorasParticipantes = MapInstitutions(proyecto.EntidadesEjecutorasParticipantes),
        SectoresEstrategicos = MapNomencladores(proyecto.SectoresEstrategicos),
        EjesEstrategicos = MapNomencladores(proyecto.EjesEstrategicos),
        Empresas = MapInstitutions(proyecto.Empresas),
        PublicacionesDerivadas = GetPublicacionesDerivadas(proyecto)
    };

    private static ProyectoApoyoProgramaDto MapApoyoProgramaDto(ProyectoApoyoPrograma proyecto) => new()
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
        Participantes = MapParticipantes(proyecto.Participantes),
        FechaInicio = proyecto.FechaInicio,
        FechaCierre = proyecto.FechaCierre,
        CodigoProyecto = proyecto.CodigoProyecto,
        TributaDesarrolloLocal = proyecto.TributaDesarrolloLocal,
        EstadosDeEjecucion = MapNomencladores(proyecto.EstadosDeEjecucion),
        EntidadesEjecutorasPrincipales = MapInstitutions(proyecto.EntidadesEjecutorasPrincipales),
        EntidadesEjecutorasParticipantes = MapInstitutions(proyecto.EntidadesEjecutorasParticipantes),
        SectoresEstrategicos = MapNomencladores(proyecto.SectoresEstrategicos),
        EjesEstrategicos = MapNomencladores(proyecto.EjesEstrategicos),
        Programas = proyecto.Programas.Select(p => new NomencladorDto(p.Id, p.Nombre)).ToList(),
        TipoPAP = proyecto.TipoPAP,
        PublicacionesDerivadas = GetPublicacionesDerivadas(proyecto)
    };

    private static ProyectoDesarrolloLocalDto MapDesarrolloLocalDto(ProyectoDesarrolloLocal proyecto) => new()
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
        Participantes = MapParticipantes(proyecto.Participantes),
        FechaInicio = proyecto.FechaInicio,
        FechaCierre = proyecto.FechaCierre,
        CodigoProyecto = proyecto.CodigoProyecto,
        TributaDesarrolloLocal = proyecto.TributaDesarrolloLocal,
        EstadosDeEjecucion = MapNomencladores(proyecto.EstadosDeEjecucion),
        EntidadesEjecutorasPrincipales = MapInstitutions(proyecto.EntidadesEjecutorasPrincipales),
        EntidadesEjecutorasParticipantes = MapInstitutions(proyecto.EntidadesEjecutorasParticipantes),
        SectoresEstrategicos = MapNomencladores(proyecto.SectoresEstrategicos),
        EjesEstrategicos = MapNomencladores(proyecto.EjesEstrategicos),
        MunicipioId = proyecto.MunicipioId,
        MunicipioNombre = proyecto.Municipio?.Nombre ?? string.Empty,
        ProvinciaNombre = proyecto.Municipio?.Provincia?.Nombre,
        PublicacionesDerivadas = GetPublicacionesDerivadas(proyecto)
    };

    private static ProyectoNoEmpresarialDto MapNoEmpresarialDto(ProyectoNoEmpresarial proyecto) => new()
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
        Participantes = MapParticipantes(proyecto.Participantes),
        FechaInicio = proyecto.FechaInicio,
        FechaCierre = proyecto.FechaCierre,
        CodigoProyecto = proyecto.CodigoProyecto,
        TributaDesarrolloLocal = proyecto.TributaDesarrolloLocal,
        EstadosDeEjecucion = MapNomencladores(proyecto.EstadosDeEjecucion),
        EntidadesEjecutorasPrincipales = MapInstitutions(proyecto.EntidadesEjecutorasPrincipales),
        EntidadesEjecutorasParticipantes = MapInstitutions(proyecto.EntidadesEjecutorasParticipantes),
        SectoresEstrategicos = MapNomencladores(proyecto.SectoresEstrategicos),
        EjesEstrategicos = MapNomencladores(proyecto.EjesEstrategicos),
        Entidades = MapInstitutions(proyecto.Entidades),
        PublicacionesDerivadas = GetPublicacionesDerivadas(proyecto)
    };

    private static ProyectoColabInternacionalDto MapColabInternacionalDto(ProyectoColabInternacional proyecto) => new()
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
        Participantes = MapParticipantes(proyecto.Participantes),
        FechaInicio = proyecto.FechaInicio,
        FechaCierre = proyecto.FechaCierre,
        CodigoProyecto = proyecto.CodigoProyecto,
        TributaDesarrolloLocal = proyecto.TributaDesarrolloLocal,
        EstadosDeEjecucion = MapNomencladores(proyecto.EstadosDeEjecucion),
        EntidadesEjecutorasPrincipales = MapInstitutions(proyecto.EntidadesEjecutorasPrincipales),
        EntidadesEjecutorasParticipantes = MapInstitutions(proyecto.EntidadesEjecutorasParticipantes),
        SectoresEstrategicos = MapNomencladores(proyecto.SectoresEstrategicos),
        EjesEstrategicos = MapNomencladores(proyecto.EjesEstrategicos),
        FuentesFinanciacion = MapNomencladores(proyecto.FuentesFinanciacion),
        TerminosReferencia = proyecto.TerminosReferencia,
        PublicacionesDerivadas = GetPublicacionesDerivadas(proyecto)
    };

    private static ProyectoPNAPDto MapPnapDto(ProyectoPNAP proyecto) => new()
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
        Participantes = MapParticipantes(proyecto.Participantes),
        FechaInicio = proyecto.FechaInicio,
        FechaCierre = proyecto.FechaCierre,
        CodigoProyecto = proyecto.CodigoProyecto,
        TributaDesarrolloLocal = proyecto.TributaDesarrolloLocal,
        EstadosDeEjecucion = MapNomencladores(proyecto.EstadosDeEjecucion),
        EntidadesEjecutorasPrincipales = MapInstitutions(proyecto.EntidadesEjecutorasPrincipales),
        EntidadesEjecutorasParticipantes = MapInstitutions(proyecto.EntidadesEjecutorasParticipantes),
        SectoresEstrategicos = MapNomencladores(proyecto.SectoresEstrategicos),
        EjesEstrategicos = MapNomencladores(proyecto.EjesEstrategicos),
        FuentesFinanciacion = MapNomencladores(proyecto.FuentesFinanciacion),
        PublicacionesDerivadas = GetPublicacionesDerivadas(proyecto)
    };

    // ── Mapping utilities ─────────────────────────────────────────────────────

    private static List<NomencladorDto> MapNomencladores<T>(ICollection<T> items)
        where T : class
    {
        // Use duck-typing via reflection-less runtime cast for the known nomenclator types.
        // All nomencladors have (int Id, string Nombre).
        return items.Select(item => item switch
        {
            EstadoProyecto e => new NomencladorDto(e.Id, e.Nombre),
            SectorEstrategico s => new NomencladorDto(s.Id, s.Nombre),
            EjeEstrategico ej => new NomencladorDto(ej.Id, ej.Nombre),
            FuenteFinanciacion f => new NomencladorDto(f.Id, f.Nombre),
            SituacionProyecto si => new NomencladorDto(si.Id, si.Nombre),
            Programa p => new NomencladorDto(p.Id, p.Nombre),
            _ => throw new InvalidOperationException($"Unrecognized nomenclator type: {item.GetType().Name}")
        }).ToList();
    }

    private static List<InstitutionRefDto> MapInstitutions(ICollection<Institution> items)
        => items.Select(i => new InstitutionRefDto(i.Id, i.Nombre)).ToList();

    private static List<UserRefDto> MapParticipantes(ICollection<User> users)
        => users.Select(u => new UserRefDto(u.Id, GetNombreCompleto(u), u.Email)).ToList();

    private static string GetNombreCompleto(User user)
        => user.UserName + " " + user.UserLastName1 + (user.UserLastName2 != null ? " " + user.UserLastName2 : "");

    private static List<string> GetPublicacionesDerivadas(Proyecto proyecto)
        => proyecto.PublicacionesDerivadas.Select(pub => pub.UrlDoi ?? pub.Title).ToList();
}
