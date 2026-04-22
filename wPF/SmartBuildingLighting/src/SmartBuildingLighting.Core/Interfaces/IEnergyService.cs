using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.Core.Interfaces;

/// <summary>
/// 能耗统计服务接口
/// </summary>
public interface IEnergyService
{
    /// <summary>记录能耗数据</summary>
    Task RecordEnergyAsync(int circuitId, float powerConsumption);

    /// <summary>获取指定时间范围内的能耗数据</summary>
    Task<List<EnergyRecord>> GetEnergyRecordsAsync(DateTime startTime, DateTime endTime);

    /// <summary>按日统计能耗</summary>
    Task<Dictionary<DateTime, float>> GetDailyEnergyAsync(DateTime startDate, DateTime endDate);

    /// <summary>按楼层统计能耗</summary>
    Task<Dictionary<string, float>> GetEnergyByFloorAsync(DateTime startDate, DateTime endDate);

    /// <summary>获取回路能耗排行</summary>
    Task<List<(string CircuitName, float TotalEnergy)>> GetTopEnergyCircuitsAsync(int topN, DateTime startDate, DateTime endDate);

    /// <summary>获取今日总能耗</summary>
    Task<float> GetTodayTotalEnergyAsync();

    /// <summary>导出能耗数据为CSV</summary>
    Task<string> ExportToCsvAsync(DateTime startDate, DateTime endDate);

    /// <summary>按当前回路功率采样并累计能耗</summary>
    Task SampleAsync(IEnumerable<LightCircuit> circuits, DateTime sampledAt);
}
