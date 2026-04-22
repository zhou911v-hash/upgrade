using Microsoft.EntityFrameworkCore;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;
using SmartBuildingLighting.Data;

namespace SmartBuildingLighting.Services;

/// <summary>
/// 情景模式业务服务
/// </summary>
public class SceneModeService : ISceneModeService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILightCircuitService _circuitService;

    public SceneModeService(IDbContextFactory<AppDbContext> contextFactory, ILightCircuitService circuitService)
    {
        _contextFactory = contextFactory;
        _circuitService = circuitService;
    }

    public async Task<List<SceneMode>> GetAllModesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SceneModes
            .Include(m => m.Details)
            .OrderBy(m => m.ModeType)
            .ThenBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<SceneMode?> GetModeByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SceneModes
            .Include(m => m.Details)
                .ThenInclude(d => d.Circuit)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<bool> ApplyModeAsync(int modeId)
    {
        var mode = await GetModeByIdAsync(modeId);
        if (mode == null) return false;

        // 按 "关闭" 和 "亮度档位" 分组批量执行，把 N 次单独调用压到 1 + k 次（k = 不同亮度档数）
        var offIds = mode.Details
            .Where(d => !d.TargetState)
            .Select(d => d.CircuitId)
            .ToList();

        bool overallSuccess = true;

        if (offIds.Count > 0)
        {
            bool offOk = await _circuitService.BatchControlAsync(offIds, false);
            overallSuccess &= offOk;
        }

        var brightnessGroups = mode.Details
            .Where(d => d.TargetState)
            .GroupBy(d => Math.Clamp(d.TargetBrightness, 0, 100));

        foreach (var group in brightnessGroups)
        {
            var ids = group.Select(d => d.CircuitId).ToList();
            if (ids.Count == 0)
                continue;
            bool groupOk = await _circuitService.BatchSetBrightnessAsync(ids, group.Key);
            overallSuccess &= groupOk;
        }

        return overallSuccess;
    }

    public async Task<SceneMode> CreateModeAsync(SceneMode mode)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.SceneModes.Add(mode);
        await context.SaveChangesAsync();
        return mode;
    }

    public async Task<bool> UpdateModeAsync(SceneMode mode)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.SceneModes
            .Include(m => m.Details)
            .FirstOrDefaultAsync(m => m.Id == mode.Id);

        if (existing == null) return false;

        existing.Name = mode.Name;
        existing.Description = mode.Description;
        existing.IconName = mode.IconName;

        // 先删除旧明细，再添加新的
        context.SceneModeDetails.RemoveRange(existing.Details);
        foreach (var detail in mode.Details)
        {
            detail.SceneModeId = existing.Id;
            context.SceneModeDetails.Add(detail);
        }

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteModeAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var mode = await context.SceneModes.FindAsync(id);
        if (mode == null) return false;

        context.SceneModes.Remove(mode);
        await context.SaveChangesAsync();
        return true;
    }
}
