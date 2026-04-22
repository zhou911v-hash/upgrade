using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.Services.Communication;

internal interface ICommunicationBackend
{
    bool IsConnected { get; }
    Task<bool> ConnectAsync(CommunicationProfile profile, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<bool> ReadCircuitStatusAsync(int address, CancellationToken cancellationToken = default);
    Task<bool> WriteCircuitControlAsync(int address, bool state, CancellationToken cancellationToken = default);
    Task<int> ReadBrightnessAsync(int address, CancellationToken cancellationToken = default);
    Task<bool> WriteBrightnessAsync(int address, int brightness, CancellationToken cancellationToken = default);
    Task<float> ReadCurrentAsync(int address, CancellationToken cancellationToken = default);
    Task<float> ReadPowerAsync(int address, CancellationToken cancellationToken = default);
    event EventHandler<CircuitStatusChangedEventArgs>? StatusChanged;
}
