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

    public Task<List<AwardDto>> GetMyAwardsAsync(CancellationToken ct = default)
    {
        return _context.UserAwardeds
            .AsNoTracking()
            .Where(ua => ua.UserId == _currentUser.Id)
            .Select(ua => new AwardDto
            {
                Id = ua.Id,
                AwardName = ua.Award.Name,
                AwardTypeId = ua.Award.AwardTypeId,
                AwardTypeName = ua.Award.AwardType.Name,
                Year = ua.Year,
                AwardedAt = ua.AwardedAt,
            })
            .OrderByDescending(a => a.Year)
            .ThenByDescending(a => a.AwardedAt)
            .ToListAsync(ct);
    }

    public async Task<(Result Result, int? AwardedId)> CreateAsync(CreateAwardRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.AwardName))
            return (Result.Failure(new[] { "El nombre del premio es obligatorio." }), null);

        if (!await _context.AwardTypes.AnyAsync(t => t.Id == request.AwardTypeId, ct))
            return (Result.Failure(new[] { "Tipo de premio inválido." }), null);

        var award = new Award
        {
            Name = request.AwardName.Trim(),
            AwardTypeId = request.AwardTypeId,
        };
        _context.Awards.Add(award);
        await _context.SaveChangesAsync(ct);

        var userAwarded = new UserAwarded
        {
            UserId = _currentUser.Id!,
            AwardId = award.Id,
            Year = request.Year,
            AwardedAt = request.AwardedAt,
        };
        _context.UserAwardeds.Add(userAwarded);
        await _context.SaveChangesAsync(ct);

        return (Result.Success(), userAwarded.Id);
    }

    public async Task<Result> UpdateAsync(int id, UpdateAwardRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.AwardName))
            return Result.Failure(new[] { "El nombre del premio es obligatorio." });

        if (!await _context.AwardTypes.AnyAsync(t => t.Id == request.AwardTypeId, ct))
            return Result.Failure(new[] { "Tipo de premio inválido." });

        var userAwarded = await _context.UserAwardeds
            .Include(ua => ua.Award)
            .FirstOrDefaultAsync(ua => ua.Id == id, ct);

        if (userAwarded is null)
            return Result.Failure(new[] { "Premio no encontrado." });

        if (userAwarded.UserId != _currentUser.Id)
            return Result.Failure(new[] { "No tienes permiso para modificar este premio." });

        userAwarded.Award.Name = request.AwardName.Trim();
        userAwarded.Award.AwardTypeId = request.AwardTypeId;
        userAwarded.Year = request.Year;
        userAwarded.AwardedAt = request.AwardedAt;

        await _context.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var userAwarded = await _context.UserAwardeds
            .FirstOrDefaultAsync(ua => ua.Id == id, ct);

        if (userAwarded is null)
            return Result.Failure(new[] { "Premio no encontrado." });

        if (userAwarded.UserId != _currentUser.Id)
            return Result.Failure(new[] { "No tienes permiso para eliminar este premio." });

        _context.UserAwardeds.Remove(userAwarded);
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }
}
