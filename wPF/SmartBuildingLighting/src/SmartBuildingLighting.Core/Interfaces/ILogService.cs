using SmartBuildingLighting.Core.Enums;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.Core.Interfaces;

/// <summary>
/// 操作日志服务接口
/// </summary>
public interface ILogService
{
    /// <summary>记录操作日志</summary>
    Task LogAsync(OperationType type, string description, string? targetName = null, int? userId = null);

    /// <summary>记录带结果的操作日志</summary>
    Task LogAsync(OperationType type, string description, bool isSuccess, string? targetName = null, int? userId = null, string? details = null);

    /// <summary>获取日志列表（分页）</summary>
    Task<(List<OperationLog> Logs, int TotalCount)> GetLogsAsync(
        int page = 1,
        int pageSize = 20,
        OperationType? type = null,
        DateTime? startTime = null,
        DateTime? endTime = null);

    /// <summary>获取最近N条日志</summary>
    Task<List<OperationLog>> GetRecentLogsAsync(int count = 10);
}
