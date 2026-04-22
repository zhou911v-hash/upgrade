using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartBuildingLighting.Core.Enums;

namespace SmartBuildingLighting.Core.Models;

/// <summary>
/// 情景模式实体
/// </summary>
public class SceneMode
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>模式名称</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>模式描述</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>模式类型（预设/自定义）</summary>
    public SceneModeType ModeType { get; set; }

    /// <summary>图标名称</summary>
    [MaxLength(50)]
    public string IconName { get; set; } = "LightBulb";

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>模式明细</summary>
    public virtual ICollection<SceneModeDetail> Details { get; set; } = new List<SceneModeDetail>();
}
