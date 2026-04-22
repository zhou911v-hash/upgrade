using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.Core.Interfaces;

/// <summary>
/// 通信配置管理
/// </summary>
public interface ICommunicationProfileService
{
    Task<List<CommunicationProfile>> GetProfilesAsync();
    Task<CommunicationProfile?> GetActiveProfileAsync();
    Task<CommunicationProfile> SaveAsync(CommunicationProfile profile);
    Task<bool> SetActiveAsync(int profileId);
}
