using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartBuildingLighting.Core.Models;

/// <summary>
/// 区域实体（如办公室、走廊、会议室等）
/// </summary>
public class Area
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>所属楼层ID</summary>
    public int FloorId { get; set; }

    /// <summary>区域名称</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>区域类型（办公室/走廊/会议室/卫生间/大厅等）</summary>
    [MaxLength(50)]
    public string AreaType { get; set; } = string.Empty;

    /// <summary>平面图上X坐标（百分比 0-100）</summary>
    public double PositionX { get; set; }

    /// <summary>平面图上Y坐标（百分比 0-100）</summary>
    public double PositionY { get; set; }

    /// <summary>区域宽度（百分比）</summary>
    public double Width { get; set; }

    /// <summary>区域高度（百分比）</summary>
    public double Height { get; set; }

    /// <summary>所属楼层</summary>
    [ForeignKey(nameof(FloorId))]
    public virtual Floor? Floor { get; set; }

    /// <summary>该区域包含的照明回路</summary>
    public virtual ICollection<LightCircuit> LightCircuits { get; set; } = new List<LightCircuit>();
}
