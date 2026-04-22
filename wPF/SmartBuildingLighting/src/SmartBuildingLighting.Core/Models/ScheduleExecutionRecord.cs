using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartBuildingLighting.Core.Models;

/// <summary>
/// 定时任务执行记录
/// </summary>
public class ScheduleExecutionRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int ScheduleId { get; set; }

    public DateTime ExecutedAt { get; set; } = DateTime.Now;

    public bool IsSuccess { get; set; }

    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? TargetName { get; set; }

    [ForeignKey(nameof(ScheduleId))]
    public virtual Schedule? Schedule { get; set; }
}
