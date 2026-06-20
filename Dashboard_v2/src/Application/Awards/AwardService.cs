using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.Awards;

public sealed class AwardService : IAwardService
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public AwardService(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    private bool IsSuperuser => _currentUser.Roles?.Contains("Superuser") == true;

    public async Task<List<AwardWithGrantingsDto>> GetMyAwardsAsync(CancellationToken ct = default)
    {
        if (IsSuperuser)
            return await GetAllAwardsAsync(ct);

        var userAwardeds = await _context.UserAwardeds
            .AsNoTracking()
            .Include(ua => ua.Award)
                .ThenInclude(a => a.AwardType)
            .Include(ua => ua.User)
            .ToListAsync(ct);

        var grouped = userAwardeds
            .GroupBy(ua => new { ua.AwardId, ua.Award.Name, ua.Award.AwardTypeId, AwardTypeName = ua.Award.AwardType.Name })
            .Select(g => new AwardWithGrantingsDto
            {
                AwardId = g.Key.AwardId,
                AwardName = g.Key.Name,
                AwardTypeId = g.Key.AwardTypeId,
                AwardTypeName = g.Key.AwardTypeName,
                Grantings = g
                    .GroupBy(ua => ua.AwardedAt.Date)
                    .Select(gg => new GrantingDto
                    {
                        AwardedAt = gg.First().AwardedAt,
                        Recipients = gg
                            .Select(r => new RecipientDto
                            {
                                Id = r.Id,
                                UserId = r.UserId,
                                UserDisplayName = r.User.UserName + " " + r.User.UserLastName1 + (r.User.UserLastName2 ?? ""),
                                EvidenceFileId = r.EvidenceFileId,
                            })
                            .OrderBy(x => x.UserDisplayName)
                            .ToList()
                    })
                    .Where(gr => gr.Recipients.Any(rr => rr.UserId == _currentUser.Id))
                    .OrderByDescending(gr => gr.AwardedAt)
                    .ToList()
            })
            .Where(a => a.Grantings.Any())
            .ToList();

        return grouped;
    }

    public async Task<List<AwardWithGrantingsDto>> GetAllAwardsAsync(CancellationToken ct = default)
    {
        var userAwardeds = await _context.UserAwardeds
            .AsNoTracking()
            .Include(ua => ua.Award)
                .ThenInclude(a => a.AwardType)
            .Include(ua => ua.User)
            .ToListAsync(ct);

        var grouped = userAwardeds
            .GroupBy(ua => new { ua.AwardId, ua.Award.Name, ua.Award.AwardTypeId, AwardTypeName = ua.Award.AwardType.Name })
            .Select(g => new AwardWithGrantingsDto
            {
                AwardId = g.Key.AwardId,
                AwardName = g.Key.Name,
                AwardTypeId = g.Key.AwardTypeId,
                AwardTypeName = g.Key.AwardTypeName,
                Grantings = g
                    .GroupBy(ua => ua.AwardedAt.Date)
                    .Select(gg => new GrantingDto
                    {
                        AwardedAt = gg.First().AwardedAt,
                        Recipients = gg
                            .Select(r => new RecipientDto
                            {
                                Id = r.Id,
                                UserId = r.UserId,
                                UserDisplayName = r.User.UserName + " " + r.User.UserLastName1 + (r.User.UserLastName2 ?? ""),
                                EvidenceFileId = r.EvidenceFileId,
                            })
                            .OrderBy(x => x.UserDisplayName)
                            .ToList()
                    })
                    .OrderByDescending(gr => gr.AwardedAt)
                    .ToList()
            })
            .ToList();

        return grouped;
    }

    public async Task<List<AwardWithGrantingsDto>> GetAreaAwardsAsync(CancellationToken ct = default)
    {
        var areaId = await _context.GetUserAreaIdAsync(_currentUser.Id, ct) ?? string.Empty;

        var userAwardeds = await _context.UserAwardeds
            .AsNoTracking()
            .Include(ua => ua.Award)
                .ThenInclude(a => a.AwardType)
            .Include(ua => ua.User)
            .Where(ua => ua.User != null && ua.User.AreaId == areaId)
            .ToListAsync(ct);

        var grouped = userAwardeds
            .GroupBy(ua => new { ua.AwardId, ua.Award.Name, ua.Award.AwardTypeId, AwardTypeName = ua.Award.AwardType.Name })
            .Select(g => new AwardWithGrantingsDto
            {
                AwardId = g.Key.AwardId,
                AwardName = g.Key.Name,
                AwardTypeId = g.Key.AwardTypeId,
                AwardTypeName = g.Key.AwardTypeName,
                Grantings = g
                    .GroupBy(ua => ua.AwardedAt.Date)
                    .Select(gg => new GrantingDto
                    {
                        AwardedAt = gg.First().AwardedAt,
                        Recipients = gg
                            .Select(r => new RecipientDto
                            {
                                Id = r.Id,
                                UserId = r.UserId,
                                UserDisplayName = r.User.UserName + " " + r.User.UserLastName1 + (r.User.UserLastName2 ?? ""),
                                EvidenceFileId = r.EvidenceFileId,
                            })
                            .OrderBy(x => x.UserDisplayName)
                            .ToList()
                    })
                    .OrderByDescending(gr => gr.AwardedAt)
                    .ToList()
            })
            .ToList();

        return grouped;
    }

    public async Task<List<AwardCatalogDto>> GetCatalogAsync(CancellationToken ct = default)
    {
        var awards = await _context.Awards
            .AsNoTracking()
            .Select(a => new AwardCatalogDto
            {
                Id = a.Id,
                AwardName = a.Name,
                AwardTypeId = a.AwardTypeId,
                AwardTypeName = a.AwardType.Name,
            })
            .OrderBy(a => a.AwardTypeId)
            .ThenBy(a => a.AwardName)
            .ThenBy(a => a.Id)
            .ToListAsync(ct);

        return awards
            .GroupBy(a => new { a.AwardTypeId, Name = NormalizeAwardKey(a.AwardName) })
            .Select(group => group.First())
            .OrderBy(a => a.AwardTypeId)
            .ThenBy(a => a.AwardName)
            .ToList();
    }

    public async Task<(Result Result, int? AwardedId)> CreateAsync(CreateAwardRequest request, CancellationToken ct = default)
    {
        string userId;
        if (IsSuperuser)
        {
            if (string.IsNullOrWhiteSpace(request.TargetUserId))
                return (Result.Failure(new[] { "El Superuser debe especificar el usuario destinatario (TargetUserId)." }), null);
            if (!await _context.Users.AnyAsync(u => u.Id == request.TargetUserId, ct))
                return (Result.Failure(new[] { "El usuario destinatario no existe." }), null);
            userId = request.TargetUserId;
        }
        else
        {
            userId = _currentUser.Id!;
        }

        var (result, award) = await ResolveAwardAsync(request.AwardId, request.NewAwardName, request.AwardTypeId, ct);
        if (!result.Succeeded || award is null)
            return (result, null);

        var userAwarded = new UserAwarded
        {
            UserId = userId,
            AwardId = award.Id,
            AwardedAt = request.AwardedAt,
            EvidenceFileId = request.EvidenceFileId,
        };
        _context.UserAwardeds.Add(userAwarded);
        await _context.SaveChangesAsync(ct);

        return (Result.Success(), userAwarded.Id);
    }

    public async Task<Result> UpdateAsync(int id, UpdateAwardRequest request, CancellationToken ct = default)
    {
        var userAwarded = await _context.UserAwardeds
            .FirstOrDefaultAsync(ua => ua.Id == id, ct);

        if (userAwarded is null)
            return Result.Failure(new[] { "Premio no encontrado." });

        if (!IsSuperuser && userAwarded.UserId != _currentUser.Id)
            return Result.Failure(new[] { "No tienes permiso para modificar este premio." });

        var (result, award) = await ResolveAwardAsync(request.AwardId, request.NewAwardName, request.AwardTypeId, ct);
        if (!result.Succeeded || award is null)
            return result;

        userAwarded.AwardId = award.Id;
        userAwarded.AwardedAt = request.AwardedAt;
        userAwarded.EvidenceFileId = request.EvidenceFileId;

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var userAwarded = await _context.UserAwardeds
            .FirstOrDefaultAsync(ua => ua.Id == id, ct);

        if (userAwarded is null)
            return Result.Failure(new[] { "Premio no encontrado." });

        if (!IsSuperuser && userAwarded.UserId != _currentUser.Id)
            return Result.Failure(new[] { "No tienes permiso para eliminar este premio." });

        _context.UserAwardeds.Remove(userAwarded);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    private async Task<(Result Result, Award? Award)> ResolveAwardAsync(
        int? awardId,
        string? newAwardName,
        int? awardTypeId,
        CancellationToken ct)
    {
        if (awardId.HasValue)
        {
            var existingAward = await _context.Awards
                .FirstOrDefaultAsync(a => a.Id == awardId.Value, ct);

            return existingAward is null
                ? (Result.Failure(new[] { "El premio seleccionado no existe." }), null)
                : (Result.Success(), existingAward);
        }

        if (string.IsNullOrWhiteSpace(newAwardName))
            return (Result.Failure(new[] { "Debes seleccionar un premio existente o escribir uno nuevo." }), null);

        if (!awardTypeId.HasValue)
            return (Result.Failure(new[] { "Debes indicar el tipo del nuevo premio." }), null);

        if (!await _context.AwardTypes.AnyAsync(t => t.Id == awardTypeId.Value, ct))
            return (Result.Failure(new[] { "Tipo de premio inválido." }), null);

        var normalizedName = NormalizeAwardName(newAwardName);
        var normalizedLower = normalizedName.ToLower();

        var existingByName = await _context.Awards
            .FirstOrDefaultAsync(a =>
                a.AwardTypeId == awardTypeId.Value &&
                a.Name.ToLower() == normalizedLower, ct);

        if (existingByName is not null)
            return (Result.Success(), existingByName);

        var newAward = new Award
        {
            Name = normalizedName,
            AwardTypeId = awardTypeId.Value,
        };

        _context.Awards.Add(newAward);
        await _context.SaveChangesAsync(ct);

        return (Result.Success(), newAward);
    }

    private static string NormalizeAwardName(string awardName)
        => awardName.Trim();

    private static string NormalizeAwardKey(string awardName)
        => NormalizeAwardName(awardName).ToUpperInvariant();
}
