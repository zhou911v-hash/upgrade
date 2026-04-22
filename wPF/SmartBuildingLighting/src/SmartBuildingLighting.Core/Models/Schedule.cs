using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartBuildingLighting.Core.Enums;

namespace SmartBuildingLighting.Core.Models;

/// <summary>
/// 定时任务实体
/// </summary>
public class Schedule
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>任务名称</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>目标回路ID（可为空表示全局）</summary>
    public int? CircuitId { get; set; }

    /// <summary>目标分组ID（可为空）</summary>
    public int? GroupId { get; set; }

    /// <summary>目标情景模式ID（可为空）</summary>
    public int? SceneModeId { get; set; }

    /// <summary>目标类型</summary>
    public ScheduleTargetType TargetType { get; set; } = ScheduleTargetType.Circuit;

    /// <summary>Cron 表达式</summary>
    [Required]
    [MaxLength(100)]
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>目标开关状态</summary>
    public bool TargetState { get; set; }

    /// <summary>目标亮度（0-100）</summary>
    public int TargetBrightness { get; set; } = 100;

    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>任务描述</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>上次执行时间</summary>
    public DateTime? LastExecutedAt { get; set; }

    /// <summary>最近执行结果</summary>
    [MaxLength(500)]
    public string? LastExecutionMessage { get; set; }

    [ForeignKey(nameof(CircuitId))]
    public virtual LightCircuit? Circuit { get; set; }

    [ForeignKey(nameof(GroupId))]
    public virtual LightGroup? Group { get; set; }

    [ForeignKey(nameof(SceneModeId))]
    public virtual SceneMode? SceneMode { get; set; }

    public virtual ICollection<ScheduleExecutionRecord> ExecutionRecords { get; set; } = new List<ScheduleExecutionRecord>();
}
