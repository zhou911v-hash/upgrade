using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.Core.Interfaces;

/// <summary>
/// 通信服务接口 — 与照明控制器通信
/// </summary>
public interface ICommunicationService
{
    /// <summary>当前激活的通信配置</summary>
    CommunicationProfile? ActiveProfile { get; }

    /// <summary>连接到控制器</summary>
    Task<bool> ConnectAsync(CommunicationProfile profile, CancellationToken cancellationToken = default);

    /// <summary>使用当前配置重连</summary>
    Task<bool> ReconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>断开连接</summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>是否已连接</summary>
    bool IsConnected { get; }

    /// <summary>读取回路开关状态</summary>
    Task<bool> ReadCircuitStatusAsync(int address, CancellationToken cancellationToken = default);

    /// <summary>写入回路控制指令</summary>
    Task<bool> WriteCircuitControlAsync(int address, bool state, CancellationToken cancellationToken = default);

    /// <summary>读取亮度</summary>
    Task<int> ReadBrightnessAsync(int address, CancellationToken cancellationToken = default);

    /// <summary>写入亮度</summary>
    Task<bool> WriteBrightnessAsync(int address, int brightness, CancellationToken cancellationToken = default);

    /// <summary>读取电流</summary>
    Task<float> ReadCurrentAsync(int address, CancellationToken cancellationToken = default);

    /// <summary>读取功率</summary>
    Task<float> ReadPowerAsync(int address, CancellationToken cancellationToken = default);

    /// <summary>状态变化事件</summary>
    event EventHandler<CircuitStatusChangedEventArgs>? StatusChanged;

    /// <summary>连接状态变化事件</summary>
    event EventHandler<CommunicationStateChangedEventArgs>? ConnectionStateChanged;
}

/// <summary>
/// 回路状态变化事件参数
/// </summary>
public class CircuitStatusChangedEventArgs : EventArgs
{
    public int Address { get; set; }
    public bool IsOn { get; set; }
    public int Brightness { get; set; }
    public float Current { get; set; }
    public float Power { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

public class CommunicationStateChangedEventArgs : EventArgs
{
    public bool IsConnected { get; set; }
    public string Message { get; set; } = string.Empty;
    public CommunicationProfile? Profile { get; set; }
}
