namespace SmartBuildingLighting.Core.Enums;

/// <summary>
/// 操作类型枚举
/// </summary>
public enum OperationType
{
    /// <summary>登录</summary>
    Login = 0,
    /// <summary>登出</summary>
    Logout = 1,
    /// <summary>开灯</summary>
    TurnOn = 2,
    /// <summary>关灯</summary>
    TurnOff = 3,
    /// <summary>调节亮度</summary>
    AdjustBrightness = 4,
    /// <summary>应用情景模式</summary>
    ApplySceneMode = 5,
    /// <summary>分组控制</summary>
    GroupControl = 6,
    /// <summary>创建定时任务</summary>
    CreateSchedule = 7,
    /// <summary>修改定时任务</summary>
    ModifySchedule = 8,
    /// <summary>删除定时任务</summary>
    DeleteSchedule = 9,
    /// <summary>系统设置变更</summary>
    SystemSetting = 10,
    /// <summary>通信连接</summary>
    Communication = 11,
    /// <summary>调度执行</summary>
    ExecuteSchedule = 12,
    /// <summary>创建情景模式</summary>
    CreateSceneMode = 13,
    /// <summary>修改情景模式</summary>
    UpdateSceneMode = 14,
    /// <summary>删除情景模式</summary>
    DeleteSceneMode = 15,
    /// <summary>更新分组</summary>
    UpdateGroup = 16,
    /// <summary>系统告警</summary>
    Alert = 17
}
