using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.Services.Communication;

/// <summary>
/// 照明控制器模拟器 — 模拟 Modbus TCP 设备响应，用于无实际硬件时的演示。
/// 线程安全：所有对 _devices 的读写都由 _deviceLock 保护。
/// </summary>
public class SimulatorService : ICommunicationBackend
{
    private const int DeviceCount = 100;
    private const float Voltage = 220f;

    private readonly Dictionary<int, SimulatedDevice> _devices = new();
    private readonly Random _random = new(42);
    // Timer 回调和 UI 写入会并发，必须上锁
    private readonly object _deviceLock = new();
    private volatile bool _isConnected;
    private Timer? _statusTimer;
    private CommunicationProfile? _profile;

    public bool IsConnected => _isConnected;
    public event EventHandler<CircuitStatusChangedEventArgs>? StatusChanged;

    private sealed class SimulatedDevice
    {
        public int Address { get; set; }
        public bool IsOn { get; set; }
        public int Brightness { get; set; }
        public float Current { get; set; }
        public float Power { get; set; }
    }

    public SimulatorService()
    {
        for (int i = 1; i <= DeviceCount; i++)
        {
            _devices[i] = new SimulatedDevice
            {
                Address = i,
                IsOn = false,
                Brightness = 0,
                Current = 0,
                Power = 0
            };
        }
    }

    public Task<bool> ConnectAsync(CommunicationProfile profile, CancellationToken cancellationToken = default)
    {
        _profile = profile;
        _isConnected = true;

        int intervalMs = Math.Max(5, profile.TelemetryIntervalSeconds) * 1000;
        _statusTimer?.Dispose();
        _statusTimer = new Timer(_ => SafeSimulateStatusChanges(), null, intervalMs, intervalMs);

        return Task.FromResult(true);
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _isConnected = false;
        _statusTimer?.Dispose();
        _statusTimer = null;
        return Task.CompletedTask;
    }

    public Task<bool> ReadCircuitStatusAsync(int address, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
            return Task.FromResult(false);

        lock (_deviceLock)
        {
            return Task.FromResult(_devices.TryGetValue(address, out var device) && device.IsOn);
        }
    }

    public Task<bool> WriteCircuitControlAsync(int address, bool state, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
            return Task.FromResult(false);

        CircuitStatusChangedEventArgs? snapshot = null;
        lock (_deviceLock)
        {
            if (!_devices.TryGetValue(address, out var device))
                return Task.FromResult(false);

            device.IsOn = state;
            if (state)
            {
                if (device.Brightness == 0)
                    device.Brightness = 100;
                device.Current = 0.5f + (float)_random.NextDouble() * 1.5f;
                device.Power = device.Current * Voltage * (device.Brightness / 100f);
            }
            else
            {
                device.Brightness = 0;
                device.Current = 0;
                device.Power = 0;
            }

            snapshot = BuildSnapshotLocked(device);
        }

        // 事件回调在锁外触发，避免订阅方长操作把锁拖住
        if (snapshot != null)
            StatusChanged?.Invoke(this, snapshot);
        return Task.FromResult(true);
    }

    public Task<int> ReadBrightnessAsync(int address, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
            return Task.FromResult(0);

        lock (_deviceLock)
        {
            return Task.FromResult(_devices.TryGetValue(address, out var device) ? device.Brightness : 0);
        }
    }

    public Task<bool> WriteBrightnessAsync(int address, int brightness, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
            return Task.FromResult(false);

        CircuitStatusChangedEventArgs? snapshot = null;
        lock (_deviceLock)
        {
            if (!_devices.TryGetValue(address, out var device))
                return Task.FromResult(false);

            device.Brightness = Math.Clamp(brightness, 0, 100);
            if (device.Brightness > 0)
            {
                device.IsOn = true;
                float baseCurrent = 0.5f + (float)_random.NextDouble() * 1.5f;
                device.Current = baseCurrent * (device.Brightness / 100f);
                device.Power = device.Current * Voltage;
            }
            else
            {
                device.IsOn = false;
                device.Current = 0;
                device.Power = 0;
            }

            snapshot = BuildSnapshotLocked(device);
        }

        if (snapshot != null)
            StatusChanged?.Invoke(this, snapshot);
        return Task.FromResult(true);
    }

    public Task<float> ReadCurrentAsync(int address, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
            return Task.FromResult(0f);

        lock (_deviceLock)
        {
            return Task.FromResult(_devices.TryGetValue(address, out var device) ? device.Current : 0f);
        }
    }

    public Task<float> ReadPowerAsync(int address, CancellationToken cancellationToken = default)
    {
        if (!_isConnected)
            return Task.FromResult(0f);

        lock (_deviceLock)
        {
            return Task.FromResult(_devices.TryGetValue(address, out var device) ? device.Power : 0f);
        }
    }

    private void SafeSimulateStatusChanges()
    {
        try
        {
            var snapshots = new List<CircuitStatusChangedEventArgs>();
            lock (_deviceLock)
            {
                foreach (var device in _devices.Values)
                {
                    if (!device.IsOn)
                        continue;

                    // 电流微小波动 ±5%
                    float fluctuation = 1.0f + ((float)_random.NextDouble() - 0.5f) * 0.1f;
                    device.Current = Math.Max(0f, device.Current * fluctuation);
                    device.Power = device.Current * Voltage;

                    snapshots.Add(BuildSnapshotLocked(device));
                }
            }

            // 锁外触发事件
            foreach (var args in snapshots)
                StatusChanged?.Invoke(this, args);
        }
        catch
        {
            // Timer 回调吞异常，避免 TPL unhandled exception 造成进程崩溃
        }
    }

    private static CircuitStatusChangedEventArgs BuildSnapshotLocked(SimulatedDevice device) => new()
    {
        Address = device.Address,
        IsOn = device.IsOn,
        Brightness = device.Brightness,
        Current = device.Current,
        Power = device.Power,
        Timestamp = DateTime.Now
    };
}
