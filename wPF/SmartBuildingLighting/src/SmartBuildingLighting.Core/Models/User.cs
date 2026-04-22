using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartBuildingLighting.Core.Enums;

namespace SmartBuildingLighting.Core.Models;

/// <summary>
/// 用户实体
/// </summary>
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>用户名</summary>
    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    /// <summary>密码哈希值（SHA256）</summary>
    [Required]
    [MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>用户角色</summary>
    public UserRole Role { get; set; } = UserRole.User;

    /// <summary>显示名称</summary>
    [MaxLength(100)]
    public string? DisplayName { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>最后登录时间</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>操作日志</summary>
    public virtual ICollection<OperationLog> OperationLogs { get; set; } = new List<OperationLog>();
}
