using Microsoft.EntityFrameworkCore;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;
using SmartBuildingLighting.Data;

namespace SmartBuildingLighting.Services;

public class CommunicationProfileService : ICommunicationProfileService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public CommunicationProfileService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<CommunicationProfile>> GetProfilesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CommunicationProfiles
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<CommunicationProfile?> GetActiveProfileAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CommunicationProfiles
            .OrderByDescending(p => p.IsActive)
            .FirstOrDefaultAsync(p => p.IsActive);
    }

    public async Task<CommunicationProfile> SaveAsync(CommunicationProfile profile)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        CommunicationProfile entity;
        if (profile.Id == 0)
        {
            entity = profile;
            entity.CreatedAt = DateTime.Now;
            entity.UpdatedAt = DateTime.Now;
            await context.CommunicationProfiles.AddAsync(entity);
        }
        else
        {
            entity = await context.CommunicationProfiles.FindAsync(profile.Id)
                ?? throw new InvalidOperationException("通信配置不存在。");

            entity.Name = profile.Name;
            entity.Mode = profile.Mode;
            entity.UseSimulator = profile.UseSimulator;
            entity.Host = profile.Host;
            entity.Port = profile.Port;
            entity.UnitId = profile.UnitId;
            entity.CoilBaseAddress = profile.CoilBaseAddress;
            entity.BrightnessRegisterBase = profile.BrightnessRegisterBase;
            entity.CurrentRegisterBase = profile.CurrentRegisterBase;
            entity.PowerRegisterBase = profile.PowerRegisterBase;
            entity.TelemetryIntervalSeconds = profile.TelemetryIntervalSeconds;
            entity.IsActive = profile.IsActive;
            entity.UpdatedAt = DateTime.Now;
        }

        if (entity.IsActive)
        {
            var others = await context.CommunicationProfiles
                .Where(p => p.Id != entity.Id && p.IsActive)
                .ToListAsync();

            foreach (var other in others)
                other.IsActive = false;
        }

        await context.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> SetActiveAsync(int profileId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var profile = await context.CommunicationProfiles.FindAsync(profileId);
        if (profile == null)
            return false;

        var activeProfiles = await context.CommunicationProfiles.Where(p => p.IsActive).ToListAsync();
        foreach (var item in activeProfiles)
            item.IsActive = item.Id == profileId;

        if (!activeProfiles.Any(p => p.Id == profileId))
            profile.IsActive = true;

        profile.UpdatedAt = DateTime.Now;
        await context.SaveChangesAsync();
        return true;
    }
}
