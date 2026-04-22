using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartBuildingLighting.Core.Enums;

namespace SmartBuildingLighting.Core.Models;

/// <summary>
/// 照明回路实体
/// </summary>
public class LightCircuit
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>所属区域ID</summary>
    public int AreaId { get; set; }

    /// <summary>回路名称</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Modbus 寄存器地址</summary>
    [MaxLength(20)]
    public string Address { get; set; } = string.Empty;

    /// <summary>回路状态</summary>
    public CircuitStatus Status { get; set; } = CircuitStatus.Off;

    /// <summary>是否开启</summary>
    public bool IsOn { get; set; }

    /// <summary>当前电流（A）</summary>
    public float Current { get; set; }

    /// <summary>当前功率（W）</summary>
    public float Power { get; set; }

    /// <summary>亮度（0-100）</summary>
    public int Brightness { get; set; }

    /// <summary>上次更新时间</summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    /// <summary>最近一次能耗采样时间</summary>
    public DateTime? LastEnergySampleAt { get; set; }

    /// <summary>在平面图区域内的相对X位置（百分比 0-100）</summary>
    public double RelativeX { get; set; }

    /// <summary>在平面图区域内的相对Y位置（百分比 0-100）</summary>
    public double RelativeY { get; set; }

    /// <summary>所属区域</summary>
    [ForeignKey(nameof(AreaId))]
    public virtual Area? Area { get; set; }

    /// <summary>分组关联</summary>
    public virtual ICollection<LightGroupCircuit> GroupCircuits { get; set; } = new List<LightGroupCircuit>();

    /// <summary>情景模式明细</summary>
    public virtual ICollection<SceneModeDetail> SceneModeDetails { get; set; } = new List<SceneModeDetail>();

    /// <summary>能耗记录</summary>
    public virtual ICollection<EnergyRecord> EnergyRecords { get; set; } = new List<EnergyRecord>();

    /// <summary>定时任务</summary>
    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
