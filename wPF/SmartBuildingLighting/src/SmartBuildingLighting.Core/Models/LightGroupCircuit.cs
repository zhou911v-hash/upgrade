using System.ComponentModel.DataAnnotations.Schema;

namespace SmartBuildingLighting.Core.Models;

/// <summary>
/// 分组-回路 多对多关联表
/// </summary>
public class LightGroupCircuit
{
    public int GroupId { get; set; }
    public int CircuitId { get; set; }

    [ForeignKey(nameof(GroupId))]
    public virtual LightGroup? Group { get; set; }

    [ForeignKey(nameof(CircuitId))]
    public virtual LightCircuit? Circuit { get; set; }
}
