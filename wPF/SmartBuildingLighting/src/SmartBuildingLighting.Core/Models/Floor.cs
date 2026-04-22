using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartBuildingLighting.Core.Models;

/// <summary>
/// 楼层实体
/// </summary>
public class Floor
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>楼层名称（如"1楼"、"B1"）</summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>楼层编号（用于排序）</summary>
    public int FloorNumber { get; set; }

    /// <summary>楼层描述</summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>该楼层包含的区域</summary>
    public virtual ICollection<Area> Areas { get; set; } = new List<Area>();
}
