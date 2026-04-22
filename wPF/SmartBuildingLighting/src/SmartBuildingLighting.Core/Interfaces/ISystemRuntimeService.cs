namespace SmartBuildingLighting.Core.Interfaces;

/// <summary>
/// 系统运行时协调服务
/// </summary>
public interface ISystemRuntimeService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
