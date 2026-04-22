using Microsoft.Extensions.DependencyInjection;
using SmartBuildingLighting.Core.Enums;
using SmartBuildingLighting.Core.Interfaces;

namespace SmartBuildingLighting.Services;

public class SystemRuntimeService : ISystemRuntimeService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICommunicationService _communicationService;
    private readonly SemaphoreSlim _samplingLock = new(1, 1);
    private Timer? _samplingTimer;
    private bool _started;

    public SystemRuntimeService(IServiceScopeFactory scopeFactory, ICommunicationService communicationService)
    {
        _scopeFactory = scopeFactory;
        _communicationService = communicationService;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started)
            return;

        _communicationService.StatusChanged += OnStatusChanged;
        _communicationService.ConnectionStateChanged += OnConnectionStateChanged;

        using var scope = _scopeFactory.CreateScope();
        var profileService = scope.ServiceProvider.GetRequiredService<ICommunicationProfileService>();
        var scheduleService = scope.ServiceProvider.GetRequiredService<IScheduleService>();

        var activeProfile = await profileService.GetActiveProfileAsync();
        if (activeProfile != null)
        {
            await _communicationService.ConnectAsync(activeProfile, cancellationToken);
            StartSamplingTimer(activeProfile.TelemetryIntervalSeconds);
        }
        else
        {
            StartSamplingTimer(30);
        }

        await scheduleService.StartAsync();
        _started = true;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_started)
            return;

        _communicationService.StatusChanged -= OnStatusChanged;
        _communicationService.ConnectionStateChanged -= OnConnectionStateChanged;
        _samplingTimer?.Dispose();
        _samplingTimer = null;

        using var scope = _scopeFactory.CreateScope();
        var scheduleService = scope.ServiceProvider.GetRequiredService<IScheduleService>();
        await scheduleService.StopAsync();
        await _communicationService.DisconnectAsync(cancellationToken);
        _started = false;
    }

    private void StartSamplingTimer(int intervalSeconds)
    {
        int safeIntervalSeconds = Math.Max(15, intervalSeconds);
        _samplingTimer?.Dispose();
        // Timer 回调里启动独立任务并吞异常，防止 async void 造成 Unhandled Exception
        _samplingTimer = new Timer(
            _ => _ = SafeSampleAllAsync(),
            null,
            TimeSpan.FromSeconds(safeIntervalSeconds),
            TimeSpan.FromSeconds(safeIntervalSeconds));
    }

    /// <summary>
    /// 通信后端上报 · 单事件单 scope · 合并回路状态同步与能耗采样到一个 DbContext 批次
    /// </summary>
    private async void OnStatusChanged(object? sender, CircuitStatusChangedEventArgs e)
    {
        // async void 事件处理器必须自己吃掉所有异常，否则会被抛到最近的 SynchronizationContext 导致崩溃
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var circuitService = scope.ServiceProvider.GetRequiredService<ILightCircuitService>();
            var energyService = scope.ServiceProvider.GetRequiredService<IEnergyService>();
            var logService = scope.ServiceProvider.GetRequiredService<ILogService>();

            bool synced = await circuitService.SyncCircuitStatusByAddressAsync(
                e.Address, e.IsOn, e.Current, e.Power, e.Brightness);

            if (!synced)
            {
                await logService.LogAsync(
                    OperationType.Alert,
                    $"未找到地址 {e.Address:D4} 对应的照明回路。",
                    false,
                    $"地址 {e.Address:D4}");
                return;
            }

            // 重新查询已更新后的 circuit 交给 EnergyService 采样（SyncCircuit 已写入新 LastEnergySampleAt）
            var circuit = await circuitService.GetCircuitByAddressAsync(e.Address);
            if (circuit != null)
                await energyService.SampleAsync(new[] { circuit }, e.Timestamp);
        }
        catch (Exception ex)
        {
            await SafeLogAsync(OperationType.Alert, "通信状态同步失败", isSuccess: false, details: ex.Message);
        }
    }

    private async void OnConnectionStateChanged(object? sender, CommunicationStateChangedEventArgs e)
    {
        try
        {
            await SafeLogAsync(OperationType.Communication, e.Message, e.IsConnected, targetName: e.Profile?.Name);
            if (e.Profile != null && e.IsConnected)
                StartSamplingTimer(e.Profile.TelemetryIntervalSeconds);
        }
        catch (Exception ex)
        {
            await SafeLogAsync(OperationType.Alert, "连接状态事件处理失败", false, details: ex.Message);
        }
    }

    private async Task SafeSampleAllAsync()
    {
        try
        {
            await SampleAllAsync();
        }
        catch (Exception ex)
        {
            await SafeLogAsync(OperationType.Alert, "定时采样失败", false, details: ex.Message);
        }
    }

    private async Task SampleAllAsync()
    {
        if (!await _samplingLock.WaitAsync(0))
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var circuitService = scope.ServiceProvider.GetRequiredService<ILightCircuitService>();
            var energyService = scope.ServiceProvider.GetRequiredService<IEnergyService>();
            var circuits = await circuitService.GetAllCircuitsAsync();
            await energyService.SampleAsync(circuits, DateTime.Now);
        }
        finally
        {
            _samplingLock.Release();
        }
    }

    private async Task SafeLogAsync(OperationType type, string description, bool isSuccess, string? targetName = null, string? details = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<ILogService>();
            await logService.LogAsync(type, description, isSuccess, targetName, null, details);
        }
        catch
        {
            // 最后一道防线：日志也失败就不再递归，避免出现错误漩涡
        }
    }

    public void Dispose()
    {
        _samplingTimer?.Dispose();
        _samplingLock.Dispose();
    }
}
