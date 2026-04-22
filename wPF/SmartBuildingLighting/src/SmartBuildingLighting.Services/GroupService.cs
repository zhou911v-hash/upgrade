using Microsoft.EntityFrameworkCore;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;
using SmartBuildingLighting.Data;

namespace SmartBuildingLighting.Services;

public class GroupService : IGroupService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILightCircuitService _circuitService;

    public GroupService(IDbContextFactory<AppDbContext> contextFactory, ILightCircuitService circuitService)
    {
        _contextFactory = contextFactory;
        _circuitService = circuitService;
    }

    public async Task<List<LightGroup>> GetAllGroupsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.LightGroups
            .AsNoTracking()
            .Include(g => g.GroupCircuits)
                .ThenInclude(gc => gc.Circuit)
            .OrderBy(g => g.Name)
            .ToListAsync();
    }

    public async Task<LightGroup?> GetGroupByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.LightGroups
            .AsNoTracking()
            .Include(g => g.GroupCircuits)
                .ThenInclude(gc => gc.Circuit)
                    .ThenInclude(c => c!.Area)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<LightGroup> CreateGroupAsync(string name, string? description, List<int> circuitIds)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("分组名称不能为空。", nameof(name));

        await using var context = await _contextFactory.CreateDbContextAsync();
        var group = new LightGroup { Name = name.Trim(), Description = description };
        context.LightGroups.Add(group);
        await context.SaveChangesAsync();

        foreach (var cid in circuitIds.Distinct())
            context.LightGroupCircuits.Add(new LightGroupCircuit { GroupId = group.Id, CircuitId = cid });
        await context.SaveChangesAsync();
        return group;
    }

    public async Task<bool> UpdateGroupAsync(int id, string name, string? description, List<int> circuitIds)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("分组名称不能为空。", nameof(name));

        await using var context = await _contextFactory.CreateDbContextAsync();
        var group = await context.LightGroups
            .Include(g => g.GroupCircuits)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (group == null)
            return false;

        group.Name = name.Trim();
        group.Description = description;
        context.LightGroupCircuits.RemoveRange(group.GroupCircuits);
        foreach (var cid in circuitIds.Distinct())
            context.LightGroupCircuits.Add(new LightGroupCircuit { GroupId = group.Id, CircuitId = cid });
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteGroupAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var group = await context.LightGroups.FindAsync(id);
        if (group == null)
            return false;
        context.LightGroups.Remove(group);
        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 批量控制 · 直接转发给 LightCircuitService 的批处理方法，避免逐个循环 SaveChanges
    /// </summary>
    public async Task<bool> ControlGroupAsync(int groupId, bool state)
    {
        var circuitIds = await GetCircuitIdsAsync(groupId);
        if (circuitIds.Count == 0)
            return false;
        return await _circuitService.BatchControlAsync(circuitIds, state);
    }

    /// <summary>
    /// 批量设置亮度 · 批处理替代逐个写
    /// </summary>
    public async Task<bool> SetGroupBrightnessAsync(int groupId, int brightness)
    {
        var circuitIds = await GetCircuitIdsAsync(groupId);
        if (circuitIds.Count == 0)
            return false;
        return await _circuitService.BatchSetBrightnessAsync(circuitIds, brightness);
    }

    private async Task<List<int>> GetCircuitIdsAsync(int groupId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.LightGroupCircuits
            .AsNoTracking()
            .Where(gc => gc.GroupId == groupId)
            .Select(gc => gc.CircuitId)
            .ToListAsync();
    }
}
