using Microsoft.EntityFrameworkCore;
using SmartBuildingLighting.Core.Enums;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;
using SmartBuildingLighting.Data;

namespace SmartBuildingLighting.Services;

/// <summary>
/// 操作日志服务实现
/// </summary>
public class LogService : ILogService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public LogService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task LogAsync(OperationType type, string description, string? targetName = null, int? userId = null)
    {
        await LogAsync(type, description, true, targetName, userId, null);
    }

    public async Task LogAsync(OperationType type, string description, bool isSuccess, string? targetName = null, int? userId = null, string? details = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var log = new OperationLog
        {
            UserId = userId,
            OperationType = type,
            Description = description,
            TargetName = targetName,
            OperationTime = DateTime.Now,
            IsSuccess = isSuccess,
            Details = details
        };

        context.OperationLogs.Add(log);
        await context.SaveChangesAsync();
    }

    public async Task<(List<OperationLog> Logs, int TotalCount)> GetLogsAsync(
        int page = 1,
        int pageSize = 20,
        OperationType? type = null,
        DateTime? startTime = null,
        DateTime? endTime = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.OperationLogs
            .Include(l => l.User)
            .AsQueryable();

        if (type.HasValue)
            query = query.Where(l => l.OperationType == type.Value);

        if (startTime.HasValue)
            query = query.Where(l => l.OperationTime >= startTime.Value);

        if (endTime.HasValue)
            query = query.Where(l => l.OperationTime <= endTime.Value);

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.OperationTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (logs, totalCount);
    }

    public async Task<List<OperationLog>> GetRecentLogsAsync(int count = 10)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.OperationLogs
            .Include(l => l.User)
            .OrderByDescending(l => l.OperationTime)
            .Take(count)
            .ToListAsync();
    }
}
