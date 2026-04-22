using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.Core.Interfaces;

/// <summary>
/// 照明回路服务接口
/// </summary>
public interface ILightCircuitService
{
    /// <summary>获取所有楼层（含区域和回路）</summary>
    Task<List<Floor>> GetAllFloorsAsync();

    /// <summary>获取指定楼层的所有回路</summary>
    Task<List<LightCircuit>> GetCircuitsByFloorAsync(int floorId);

    /// <summary>获取所有回路</summary>
    Task<List<LightCircuit>> GetAllCircuitsAsync();

    /// <summary>获取指定区域的所有回路</summary>
    Task<List<LightCircuit>> GetCircuitsByAreaAsync(int areaId);

    /// <summary>获取单个回路详情</summary>
    Task<LightCircuit?> GetCircuitByIdAsync(int id);

    /// <summary>根据设备地址获取回路详情</summary>
    Task<LightCircuit?> GetCircuitByAddressAsync(int address);

    /// <summary>控制回路开关</summary>
    Task<bool> ToggleCircuitAsync(int circuitId, bool state);

    /// <summary>调节回路亮度</summary>
    Task<bool> SetBrightnessAsync(int circuitId, int brightness);

    /// <summary>批量控制回路</summary>
    Task<bool> BatchControlAsync(IEnumerable<int> circuitIds, bool state);

    /// <summary>批量调节亮度</summary>
    Task<bool> BatchSetBrightnessAsync(IEnumerable<int> circuitIds, int brightness);

    /// <summary>获取回路统计信息</summary>
    Task<CircuitStatistics> GetStatisticsAsync();

    /// <summary>获取楼层平面图快照</summary>
    Task<FloorLayoutSnapshot?> GetFloorLayoutAsync(int floorId);

    /// <summary>获取全部楼层平面图快照</summary>
    Task<List<FloorLayoutSnapshot>> GetBuildingLayoutAsync();

    /// <summary>更新回路状态（来自通信层）</summary>
    Task UpdateCircuitStatusAsync(int circuitId, bool isOn, float current, float power, int brightness);

    /// <summary>根据设备地址同步回路状态（来自通信层）</summary>
    Task<bool> SyncCircuitStatusByAddressAsync(int address, bool isOn, float current, float power, int brightness);
}

/// <summary>
/// 回路统计信息
/// </summary>
public class CircuitStatistics
{
    public int TotalCount { get; set; }
    public int OnCount { get; set; }
    public int OffCount { get; set; }
    public int FaultCount { get; set; }
    public float TotalPower { get; set; }
    public int FloorCount { get; set; }
}
