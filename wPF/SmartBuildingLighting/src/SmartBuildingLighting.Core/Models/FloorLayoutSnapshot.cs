using SmartBuildingLighting.Core.Enums;

namespace SmartBuildingLighting.Core.Models;

/// <summary>
/// 楼层平面图快照
/// </summary>
public class FloorLayoutSnapshot
{
    public int FloorId { get; set; }
    public string FloorName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<AreaLayoutSnapshot> Areas { get; set; } = new();
    public List<CircuitLayoutSnapshot> Circuits { get; set; } = new();
    public FloorRealtimeSummary Summary { get; set; } = new();
}

public class AreaLayoutSnapshot
{
    public int AreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AreaType { get; set; } = string.Empty;
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public class CircuitLayoutSnapshot
{
    public int CircuitId { get; set; }
    public int AreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsOn { get; set; }
    public int Brightness { get; set; }
    public float Current { get; set; }
    public float Power { get; set; }
    public CircuitStatus Status { get; set; }
    public double RelativeX { get; set; }
    public double RelativeY { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class FloorRealtimeSummary
{
    public int TotalCount { get; set; }
    public int OnCount { get; set; }
    public int OffCount { get; set; }
    public int FaultCount { get; set; }
    public float TotalPower { get; set; }
}
