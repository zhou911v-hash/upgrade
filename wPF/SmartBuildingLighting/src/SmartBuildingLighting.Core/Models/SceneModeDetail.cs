using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartBuildingLighting.Core.Models;

/// <summary>
/// 情景模式明细实体
/// </summary>
public class SceneModeDetail
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>所属情景模式ID</summary>
    public int SceneModeId { get; set; }

    /// <summary>目标回路ID</summary>
    public int CircuitId { get; set; }

    /// <summary>目标开关状态</summary>
    public bool TargetState { get; set; }

    /// <summary>目标亮度（0-100）</summary>
    public int TargetBrightness { get; set; } = 100;

    [ForeignKey(nameof(SceneModeId))]
    public virtual SceneMode? SceneMode { get; set; }

    [ForeignKey(nameof(CircuitId))]
    public virtual LightCircuit? Circuit { get; set; }
}
