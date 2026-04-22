using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartBuildingLighting.Core.Enums;

namespace SmartBuildingLighting.Core.Models;

/// <summary>
/// 操作日志实体
/// </summary>
public class OperationLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>操作用户ID</summary>
    public int? UserId { get; set; }

    /// <summary>操作类型</summary>
    public OperationType OperationType { get; set; }

    /// <summary>操作描述</summary>
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>操作目标（如回路名、分组名等）</summary>
    [MaxLength(200)]
    public string? TargetName { get; set; }

    /// <summary>操作时间</summary>
    public DateTime OperationTime { get; set; } = DateTime.Now;

    /// <summary>是否执行成功</summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>附加详情或异常信息</summary>
    [MaxLength(2000)]
    public string? Details { get; set; }

    /// <summary>操作用户</summary>
    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
}
