using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SmartBuildingLighting.Core.Enums;

namespace SmartBuildingLighting.Core.Models;

/// <summary>
/// 通信配置
/// </summary>
public class CommunicationProfile
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = "默认通信配置";

    public CommunicationMode Mode { get; set; } = CommunicationMode.Simulator;

    public bool UseSimulator { get; set; } = true;

    [MaxLength(100)]
    public string Host { get; set; } = "127.0.0.1";

    public int Port { get; set; } = 502;

    public byte UnitId { get; set; } = 1;

    public int CoilBaseAddress { get; set; }

    public int BrightnessRegisterBase { get; set; } = 1000;

    public int CurrentRegisterBase { get; set; } = 2000;

    public int PowerRegisterBase { get; set; } = 3000;

    public int TelemetryIntervalSeconds { get; set; } = 30;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
