namespace SmartBuildingLighting.Core.Enums;

/// <summary>
/// 照明回路状态枚举
/// </summary>
public enum CircuitStatus
{
    /// <summary>关闭</summary>
    Off = 0,
    /// <summary>开启</summary>
    On = 1,
    /// <summary>故障</summary>
    Fault = 2,
    /// <summary>离线</summary>
    Offline = 3
}
