using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartBuildingLighting.Core.Models;

/// <summary>
/// 能耗记录实体
/// </summary>
public class EnergyRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>回路ID</summary>
    public int CircuitId { get; set; }

    /// <summary>功耗值（kWh）</summary>
    public float PowerConsumption { get; set; }

    /// <summary>记录时间</summary>
    public DateTime RecordTime { get; set; } = DateTime.Now;

    [ForeignKey(nameof(CircuitId))]
    public virtual LightCircuit? Circuit { get; set; }
}
