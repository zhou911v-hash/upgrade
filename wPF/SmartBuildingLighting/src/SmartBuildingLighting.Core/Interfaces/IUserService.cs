using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.Core.Interfaces;

/// <summary>
/// 用户服务接口
/// </summary>
public interface IUserService
{
    /// <summary>用户登录验证</summary>
    Task<User?> LoginAsync(string username, string password);

    /// <summary>获取当前登录用户</summary>
    User? CurrentUser { get; }

    /// <summary>设置当前用户</summary>
    void SetCurrentUser(User? user);

    /// <summary>注销</summary>
    void Logout();

    /// <summary>修改密码</summary>
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
}
