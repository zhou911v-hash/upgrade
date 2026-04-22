using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;
using SmartBuildingLighting.Data;

namespace SmartBuildingLighting.Services;

/// <summary>
/// 用户服务实现 · 含登录失败次数限制（进程内，重启重置）
/// </summary>
public class UserService : IUserService
{
    /// <summary>连续失败达到该次数后锁定</summary>
    private const int MaxFailedAttempts = 5;
    /// <summary>锁定时长</summary>
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(3);

    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    // key: 用户名（大小写不敏感）· value: 当前失败计数 + 最后失败时间
    private readonly ConcurrentDictionary<string, LoginFailureRecord> _failures
        = new(StringComparer.OrdinalIgnoreCase);

    public User? CurrentUser { get; private set; }

    public UserService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        if (IsLockedOut(username))
            return null;

        await using var context = await _contextFactory.CreateDbContextAsync();
        var passwordHash = SeedData.HashPassword(password);
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == passwordHash);

        if (user == null)
        {
            RegisterFailure(username);
            return null;
        }

        user.LastLoginAt = DateTime.Now;
        await context.SaveChangesAsync();
        CurrentUser = user;
        _failures.TryRemove(username, out _);
        return user;
    }

    public void SetCurrentUser(User? user) => CurrentUser = user;

    public void Logout() => CurrentUser = null;

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            return false;

        await using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync(userId);
        if (user == null)
            return false;

        var oldHash = SeedData.HashPassword(oldPassword);
        if (user.PasswordHash != oldHash)
            return false;

        user.PasswordHash = SeedData.HashPassword(newPassword);
        await context.SaveChangesAsync();
        if (CurrentUser?.Id == userId)
            CurrentUser.PasswordHash = user.PasswordHash;
        return true;
    }

    private bool IsLockedOut(string username)
    {
        if (!_failures.TryGetValue(username, out var record))
            return false;
        if (record.Count < MaxFailedAttempts)
            return false;
        // 到时间自动解锁
        if (DateTime.UtcNow - record.LastFailedAt >= LockoutDuration)
        {
            _failures.TryRemove(username, out _);
            return false;
        }
        return true;
    }

    private void RegisterFailure(string username)
    {
        _failures.AddOrUpdate(username,
            _ => new LoginFailureRecord(1, DateTime.UtcNow),
            (_, old) => new LoginFailureRecord(old.Count + 1, DateTime.UtcNow));
    }

    private readonly record struct LoginFailureRecord(int Count, DateTime LastFailedAt);
}
