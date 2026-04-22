using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartBuildingLighting.Core.Models;

/// <summary>
/// 照明分组实体
/// </summary>
public class LightGroup
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>分组名称</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>分组描述</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>分组内回路关联</summary>
    public virtual ICollection<LightGroupCircuit> GroupCircuits { get; set; } = new List<LightGroupCircuit>();
}
