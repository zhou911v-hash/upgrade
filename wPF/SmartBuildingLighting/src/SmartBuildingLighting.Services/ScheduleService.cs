using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using SmartBuildingLighting.Core.Enums;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;
using SmartBuildingLighting.Data;

namespace SmartBuildingLighting.Services;

public class ScheduleService : IScheduleService
{
    private static IScheduler? _sharedScheduler;
    private static readonly SemaphoreSlim SchedulerLock = new(1, 1);

    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILightCircuitService _circuitService;
    private readonly IGroupService _groupService;
    private readonly ISceneModeService _sceneModeService;
    private readonly ILogService _logService;
    private readonly IServiceProvider _serviceProvider;

    public ScheduleService(
        IDbContextFactory<AppDbContext> contextFactory,
        ILightCircuitService circuitService,
        IGroupService groupService,
        ISceneModeService sceneModeService,
        ILogService logService,
        IServiceProvider serviceProvider)
    {
        _contextFactory = contextFactory;
        _circuitService = circuitService;
        _groupService = groupService;
        _sceneModeService = sceneModeService;
        _logService = logService;
        _serviceProvider = serviceProvider;
    }

    public async Task<List<Schedule>> GetAllSchedulesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Schedules
            .Include(s => s.Circuit)
            .Include(s => s.Group)
            .Include(s => s.SceneMode)
            .Include(s => s.ExecutionRecords.OrderByDescending(r => r.ExecutedAt).Take(5))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ScheduleExecutionRecord>> GetExecutionRecordsAsync(int scheduleId, int count = 20)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ScheduleExecutionRecords
            .Where(r => r.ScheduleId == scheduleId)
            .OrderByDescending(r => r.ExecutedAt)
            .Take(count)
            .ToListAsync();
    }

    public bool ValidateCronExpression(string cronExpression) => CronExpression.IsValidExpression(cronExpression);

    public async Task<Schedule> CreateScheduleAsync(Schedule schedule)
    {
        ValidateScheduleOrThrow(schedule);
        schedule.CreatedAt = DateTime.Now;
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Schedules.Add(schedule);
        await context.SaveChangesAsync();

        if (schedule.IsEnabled)
            await ScheduleJobAsync(schedule);

        return schedule;
    }

    public async Task<bool> UpdateScheduleAsync(Schedule schedule)
    {
        ValidateScheduleOrThrow(schedule);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Schedules.FindAsync(schedule.Id);
        if (existing == null)
            return false;

        existing.Name = schedule.Name;
        existing.CronExpression = schedule.CronExpression;
        existing.TargetType = schedule.TargetType;
        existing.TargetState = schedule.TargetState;
        existing.TargetBrightness = schedule.TargetBrightness;
        existing.CircuitId = schedule.CircuitId;
        existing.GroupId = schedule.GroupId;
        existing.SceneModeId = schedule.SceneModeId;
        existing.Description = schedule.Description;
        existing.IsEnabled = schedule.IsEnabled;
        existing.LastExecutionMessage = schedule.LastExecutionMessage;
        await context.SaveChangesAsync();

        await DeleteScheduledJobAsync(existing.Id);
        if (existing.IsEnabled)
            await ScheduleJobAsync(existing);

        return true;
    }

    public async Task<bool> DeleteScheduleAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var schedule = await context.Schedules.FindAsync(id);
        if (schedule == null)
            return false;

        await DeleteScheduledJobAsync(id);
        context.Schedules.Remove(schedule);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleScheduleAsync(int id, bool enabled)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var schedule = await context.Schedules.FindAsync(id);
        if (schedule == null)
            return false;

        schedule.IsEnabled = enabled;
        await context.SaveChangesAsync();

        if (enabled)
            await ScheduleJobAsync(schedule);
        else
            await DeleteScheduledJobAsync(id);

        return true;
    }

    public async Task<bool> ExecuteScheduleAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var schedule = await context.Schedules
            .Include(s => s.Circuit)
            .Include(s => s.Group)
            .Include(s => s.SceneMode)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (schedule == null)
            return false;

        bool success;
        string targetName;
        string message;

        try
        {
            (success, targetName, message) = await ExecuteTargetAsync(schedule, cancellationToken);
        }
        catch (Exception ex)
        {
            success = false;
            targetName = ResolveTargetName(schedule);
            message = ex.Message;
        }

        schedule.LastExecutedAt = DateTime.Now;
        schedule.LastExecutionMessage = message;

        context.ScheduleExecutionRecords.Add(new ScheduleExecutionRecord
        {
            ScheduleId = schedule.Id,
            ExecutedAt = schedule.LastExecutedAt.Value,
            IsSuccess = success,
            Message = message,
            TargetName = targetName
        });

        await context.SaveChangesAsync(cancellationToken);
        await _logService.LogAsync(
            OperationType.ExecuteSchedule,
            $"执行定时任务: {schedule.Name}",
            success,
            targetName,
            null,
            message);

        return success;
    }

    public async Task StartAsync()
    {
        await SchedulerLock.WaitAsync();
        try
        {
            if (_sharedScheduler == null)
            {
                var factory = new StdSchedulerFactory();
                _sharedScheduler = await factory.GetScheduler();
                _sharedScheduler.Context["RootServiceProvider"] = _serviceProvider;
                await _sharedScheduler.Start();
            }

            await using var context = await _contextFactory.CreateDbContextAsync();
            var schedules = await context.Schedules
                .Where(s => s.IsEnabled)
                .ToListAsync();

            foreach (var schedule in schedules)
                await ScheduleJobAsync(schedule);
        }
        finally
        {
            SchedulerLock.Release();
        }
    }

    public async Task StopAsync()
    {
        await SchedulerLock.WaitAsync();
        try
        {
            if (_sharedScheduler != null)
            {
                await _sharedScheduler.Shutdown();
                _sharedScheduler = null;
            }
        }
        finally
        {
            SchedulerLock.Release();
        }
    }

    private async Task<(bool Success, string TargetName, string Message)> ExecuteTargetAsync(Schedule schedule, CancellationToken cancellationToken)
    {
        switch (schedule.TargetType)
        {
            case ScheduleTargetType.Circuit:
                if (!schedule.CircuitId.HasValue)
                    throw new InvalidOperationException("定时任务未配置目标回路。");

                bool circuitSuccess = schedule.TargetState
                    ? await _circuitService.SetBrightnessAsync(schedule.CircuitId.Value, Math.Max(schedule.TargetBrightness, 1))
                    : await _circuitService.ToggleCircuitAsync(schedule.CircuitId.Value, false);

                return (
                    circuitSuccess,
                    schedule.Circuit?.Name ?? $"回路#{schedule.CircuitId}",
                    circuitSuccess ? "回路控制已执行" : "回路控制失败");

            case ScheduleTargetType.Group:
                if (!schedule.GroupId.HasValue)
                    throw new InvalidOperationException("定时任务未配置目标分组。");

                bool groupSuccess = schedule.TargetState
                    ? await _groupService.SetGroupBrightnessAsync(schedule.GroupId.Value, Math.Max(schedule.TargetBrightness, 1))
                    : await _groupService.ControlGroupAsync(schedule.GroupId.Value, false);

                return (
                    groupSuccess,
                    schedule.Group?.Name ?? $"分组#{schedule.GroupId}",
                    groupSuccess ? "分组控制已执行" : "分组控制失败");

            case ScheduleTargetType.SceneMode:
                if (!schedule.SceneModeId.HasValue)
                    throw new InvalidOperationException("定时任务未配置目标情景模式。");

                bool sceneSuccess = await _sceneModeService.ApplyModeAsync(schedule.SceneModeId.Value);
                return (
                    sceneSuccess,
                    schedule.SceneMode?.Name ?? $"情景模式#{schedule.SceneModeId}",
                    sceneSuccess ? "情景模式已执行" : "情景模式执行失败");

            case ScheduleTargetType.AllCircuits:
                var allCircuits = await _circuitService.GetAllCircuitsAsync();
                bool allSuccess = schedule.TargetState
                    ? await _circuitService.BatchSetBrightnessAsync(allCircuits.Select(c => c.Id), Math.Max(schedule.TargetBrightness, 1))
                    : await _circuitService.BatchControlAsync(allCircuits.Select(c => c.Id), false);

                return (allSuccess, "全楼照明", allSuccess ? "全局控制已执行" : "全局控制失败");

            default:
                throw new InvalidOperationException("不支持的定时任务目标类型。");
        }
    }

    private static string ResolveTargetName(Schedule schedule)
    {
        return schedule.TargetType switch
        {
            ScheduleTargetType.Circuit => schedule.Circuit?.Name ?? "回路",
            ScheduleTargetType.Group => schedule.Group?.Name ?? "分组",
            ScheduleTargetType.SceneMode => schedule.SceneMode?.Name ?? "情景模式",
            ScheduleTargetType.AllCircuits => "全楼照明",
            _ => "未知目标"
        };
    }

    private static void ValidateScheduleOrThrow(Schedule schedule)
    {
        if (string.IsNullOrWhiteSpace(schedule.Name))
            throw new ArgumentException("任务名称不能为空。");

        if (!CronExpression.IsValidExpression(schedule.CronExpression))
            throw new ArgumentException("Cron 表达式无效。");

        switch (schedule.TargetType)
        {
            case ScheduleTargetType.Circuit when !schedule.CircuitId.HasValue:
                throw new ArgumentException("请选择目标回路。");
            case ScheduleTargetType.Group when !schedule.GroupId.HasValue:
                throw new ArgumentException("请选择目标分组。");
            case ScheduleTargetType.SceneMode when !schedule.SceneModeId.HasValue:
                throw new ArgumentException("请选择目标情景模式。");
        }
    }

    private async Task ScheduleJobAsync(Schedule schedule)
    {
        if (_sharedScheduler == null || !schedule.IsEnabled)
            return;

        await DeleteScheduledJobAsync(schedule.Id);

        var job = JobBuilder.Create<ScheduleExecutionJob>()
            .WithIdentity(GetJobKey(schedule.Id))
            .UsingJobData("ScheduleId", schedule.Id)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"trigger_{schedule.Id}")
            .WithCronSchedule(schedule.CronExpression)
            .Build();

        await _sharedScheduler.ScheduleJob(job, trigger);
    }

    private static async Task DeleteScheduledJobAsync(int scheduleId)
    {
        if (_sharedScheduler == null)
            return;

        await _sharedScheduler.DeleteJob(GetJobKey(scheduleId));
    }

    private static JobKey GetJobKey(int scheduleId) => new($"schedule_{scheduleId}");
}

public class ScheduleExecutionJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        int scheduleId = context.MergedJobDataMap.GetInt("ScheduleId");
        if (context.Scheduler.Context.Get("RootServiceProvider") is not IServiceProvider rootProvider)
            return;

        using var scope = rootProvider.CreateScope();
        var scheduleService = scope.ServiceProvider.GetRequiredService<IScheduleService>();
        await scheduleService.ExecuteScheduleAsync(scheduleId, context.CancellationToken);
    }
}
