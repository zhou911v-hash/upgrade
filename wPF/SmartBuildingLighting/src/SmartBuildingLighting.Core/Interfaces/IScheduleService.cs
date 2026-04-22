using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.Core.Interfaces;

/// <summary>
/// 定时调度服务接口
/// </summary>
public interface IScheduleService
{
    /// <summary>获取所有定时任务</summary>
    Task<List<Schedule>> GetAllSchedulesAsync();

    /// <summary>获取任务执行记录</summary>
    Task<List<ScheduleExecutionRecord>> GetExecutionRecordsAsync(int scheduleId, int count = 20);

    /// <summary>校验 Cron 表达式</summary>
    bool ValidateCronExpression(string cronExpression);

    /// <summary>创建定时任务</summary>
    Task<Schedule> CreateScheduleAsync(Schedule schedule);

    /// <summary>更新定时任务</summary>
    Task<bool> UpdateScheduleAsync(Schedule schedule);

    /// <summary>删除定时任务</summary>
    Task<bool> DeleteScheduleAsync(int id);

    /// <summary>启用/禁用定时任务</summary>
    Task<bool> ToggleScheduleAsync(int id, bool enabled);

    /// <summary>执行指定任务</summary>
    Task<bool> ExecuteScheduleAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>启动调度引擎</summary>
    Task StartAsync();

    /// <summary>停止调度引擎</summary>
    Task StopAsync();
}
