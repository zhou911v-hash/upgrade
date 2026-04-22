using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.Core.Interfaces;

/// <summary>
/// 情景模式服务接口
/// </summary>
public interface ISceneModeService
{
    /// <summary>获取所有情景模式</summary>
    Task<List<SceneMode>> GetAllModesAsync();

    /// <summary>获取情景模式详情</summary>
    Task<SceneMode?> GetModeByIdAsync(int id);

    /// <summary>应用情景模式</summary>
    Task<bool> ApplyModeAsync(int modeId);

    /// <summary>创建自定义情景模式</summary>
    Task<SceneMode> CreateModeAsync(SceneMode mode);

    /// <summary>更新情景模式</summary>
    Task<bool> UpdateModeAsync(SceneMode mode);

    /// <summary>删除情景模式</summary>
    Task<bool> DeleteModeAsync(int id);
}
