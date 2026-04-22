using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.Services.Communication;

/// <summary>
/// 统一通信门面，根据配置切换模拟器或 Modbus TCP。
/// </summary>
public class CommunicationService : ICommunicationService
{
    private readonly SimulatorService _simulatorService;
    private readonly ModbusTcpCommunicationService _modbusTcpService;
    private ICommunicationBackend? _currentBackend;

    public CommunicationService(SimulatorService simulatorService, ModbusTcpCommunicationService modbusTcpService)
    {
        _simulatorService = simulatorService;
        _modbusTcpService = modbusTcpService;

        _simulatorService.StatusChanged += RelayStatusChanged;
        _modbusTcpService.StatusChanged += RelayStatusChanged;
    }

    public CommunicationProfile? ActiveProfile { get; private set; }

    public bool IsConnected => _currentBackend?.IsConnected == true;

    public event EventHandler<CircuitStatusChangedEventArgs>? StatusChanged;

    public event EventHandler<CommunicationStateChangedEventArgs>? ConnectionStateChanged;

    public async Task<bool> ConnectAsync(CommunicationProfile profile, CancellationToken cancellationToken = default)
    {
        if (_currentBackend?.IsConnected == true)
            await _currentBackend.DisconnectAsync(cancellationToken);

        ActiveProfile = profile;
        _currentBackend = ResolveBackend(profile);

        bool connected = await _currentBackend.ConnectAsync(profile, cancellationToken);
        ConnectionStateChanged?.Invoke(this, new CommunicationStateChangedEventArgs
        {
            IsConnected = connected,
            Message = connected
                ? $"已连接到{(profile.UseSimulator || profile.Mode == Core.Enums.CommunicationMode.Simulator ? "模拟器" : "Modbus TCP 设备")} {profile.Host}:{profile.Port}"
                : "连接失败",
            Profile = profile
        });

        return connected;
    }

    public async Task<bool> ReconnectAsync(CancellationToken cancellationToken = default)
    {
        if (ActiveProfile == null)
            return false;

        return await ConnectAsync(ActiveProfile, cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_currentBackend != null)
            await _currentBackend.DisconnectAsync(cancellationToken);

        ConnectionStateChanged?.Invoke(this, new CommunicationStateChangedEventArgs
        {
            IsConnected = false,
            Message = "通信连接已断开",
            Profile = ActiveProfile
        });
    }

    public Task<bool> ReadCircuitStatusAsync(int address, CancellationToken cancellationToken = default)
        => _currentBackend?.ReadCircuitStatusAsync(address, cancellationToken) ?? Task.FromResult(false);

    public Task<bool> WriteCircuitControlAsync(int address, bool state, CancellationToken cancellationToken = default)
        => _currentBackend?.WriteCircuitControlAsync(address, state, cancellationToken) ?? Task.FromResult(false);

    public Task<int> ReadBrightnessAsync(int address, CancellationToken cancellationToken = default)
        => _currentBackend?.ReadBrightnessAsync(address, cancellationToken) ?? Task.FromResult(0);

    public Task<bool> WriteBrightnessAsync(int address, int brightness, CancellationToken cancellationToken = default)
        => _currentBackend?.WriteBrightnessAsync(address, brightness, cancellationToken) ?? Task.FromResult(false);

    public Task<float> ReadCurrentAsync(int address, CancellationToken cancellationToken = default)
        => _currentBackend?.ReadCurrentAsync(address, cancellationToken) ?? Task.FromResult(0f);

    public Task<float> ReadPowerAsync(int address, CancellationToken cancellationToken = default)
        => _currentBackend?.ReadPowerAsync(address, cancellationToken) ?? Task.FromResult(0f);

    private ICommunicationBackend ResolveBackend(CommunicationProfile profile)
    {
        return profile.UseSimulator || profile.Mode == Core.Enums.CommunicationMode.Simulator
            ? _simulatorService
            : _modbusTcpService;
    }

    private void RelayStatusChanged(object? sender, CircuitStatusChangedEventArgs e)
    {
        StatusChanged?.Invoke(this, e);
    }
}
