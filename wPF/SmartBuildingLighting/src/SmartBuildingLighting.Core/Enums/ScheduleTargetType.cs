namespace SmartBuildingLighting.Core.Enums;

/// <summary>
/// 定时任务目标类型
/// </summary>
public enum ScheduleTargetType
{
    /// <summary>单个回路</summary>
    Circuit = 0,
    /// <summary>分组</summary>
    Group = 1,
    /// <summary>情景模式</summary>
    SceneMode = 2,
    /// <summary>全部回路</summary>
    AllCircuits = 3
}
