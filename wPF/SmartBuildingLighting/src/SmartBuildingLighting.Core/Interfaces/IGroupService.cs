using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.Core.Interfaces;

/// <summary>
/// 分组控制服务接口
/// </summary>
public interface IGroupService
{
    /// <summary>获取所有分组</summary>
    Task<List<LightGroup>> GetAllGroupsAsync();

    /// <summary>获取分组详情（包含回路）</summary>
    Task<LightGroup?> GetGroupByIdAsync(int id);

    /// <summary>创建分组</summary>
    Task<LightGroup> CreateGroupAsync(string name, string? description, List<int> circuitIds);

    /// <summary>更新分组</summary>
    Task<bool> UpdateGroupAsync(int id, string name, string? description, List<int> circuitIds);

    /// <summary>删除分组</summary>
    Task<bool> DeleteGroupAsync(int id);

    /// <summary>分组批量开关控制</summary>
    Task<bool> ControlGroupAsync(int groupId, bool state);

    /// <summary>分组亮度调节</summary>
    Task<bool> SetGroupBrightnessAsync(int groupId, int brightness);
}
