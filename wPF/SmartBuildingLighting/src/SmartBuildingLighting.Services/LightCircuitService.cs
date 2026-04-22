using Microsoft.EntityFrameworkCore;
using SmartBuildingLighting.Core.Enums;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;
using SmartBuildingLighting.Data;

namespace SmartBuildingLighting.Services;

/// <summary>
/// 照明回路业务服务
/// </summary>
public class LightCircuitService : ILightCircuitService
{
    private const int DefaultBrightness = 100;

    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICommunicationService _commService;

    public LightCircuitService(IDbContextFactory<AppDbContext> contextFactory, ICommunicationService commService)
    {
        _contextFactory = contextFactory;
        _commService = commService;
    }

    public async Task<List<Floor>> GetAllFloorsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Floors
            .AsNoTracking()
            .Include(f => f.Areas.OrderBy(a => a.PositionY).ThenBy(a => a.PositionX))
                .ThenInclude(a => a.LightCircuits.OrderBy(c => c.Name))
            .OrderBy(f => f.FloorNumber)
            .ToListAsync();
    }

    public async Task<List<LightCircuit>> GetAllCircuitsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.LightCircuits
            .AsNoTracking()
            .Include(c => c.Area)
                .ThenInclude(a => a!.Floor)
            .OrderBy(c => c.Address)
            .ToListAsync();
    }

    public async Task<List<LightCircuit>> GetCircuitsByFloorAsync(int floorId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.LightCircuits
            .AsNoTracking()
            .Include(c => c.Area)
                .ThenInclude(a => a!.Floor)
            .Where(c => c.Area != null && c.Area.FloorId == floorId)
            .OrderBy(c => c.Area!.PositionY)
            .ThenBy(c => c.Area!.PositionX)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<List<LightCircuit>> GetCircuitsByAreaAsync(int areaId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.LightCircuits
            .AsNoTracking()
            .Include(c => c.Area)
            .Where(c => c.AreaId == areaId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<LightCircuit?> GetCircuitByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.LightCircuits
            .AsNoTracking()
            .Include(c => c.Area)
                .ThenInclude(a => a!.Floor)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<LightCircuit?> GetCircuitByAddressAsync(int address)
    {
        string addressKey = FormatAddress(address);
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.LightCircuits
            .AsNoTracking()
            .Include(c => c.Area)
                .ThenInclude(a => a!.Floor)
            .FirstOrDefaultAsync(c => c.Address == addressKey);
    }

    public async Task<bool> ToggleCircuitAsync(int circuitId, bool state)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var circuit = await context.LightCircuits.FindAsync(circuitId);
        if (circuit == null)
            return false;

        if (!TryParseAddress(circuit.Address, out int address))
            return false;

        bool success = await _commService.WriteCircuitControlAsync(address, state);
        if (!success)
            return false;

        var telemetry = await ReadTelemetryIfOnAsync(address, state);
        int targetBrightness = state
            ? (circuit.Brightness > 0 ? circuit.Brightness : DefaultBrightness)
            : 0;

        ApplyCircuitState(circuit, state, telemetry.Current, telemetry.Power, targetBrightness);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetBrightnessAsync(int circuitId, int brightness)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var circuit = await context.LightCircuits.FindAsync(circuitId);
        if (circuit == null)
            return false;

        if (!TryParseAddress(circuit.Address, out int address))
            return false;

        int normalized = Math.Clamp(brightness, 0, 100);
        bool success = await _commService.WriteBrightnessAsync(address, normalized);
        if (!success)
            return false;

        bool state = normalized > 0;
        var telemetry = await ReadTelemetryIfOnAsync(address, state);
        ApplyCircuitState(circuit, state, telemetry.Current, telemetry.Power, normalized);
        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 批量开关控制 · 单事务单 SaveChanges · 通信环节顺序执行避免底层竞争
    /// </summary>
    public async Task<bool> BatchControlAsync(IEnumerable<int> circuitIds, bool state)
    {
        var ids = circuitIds?.Distinct().ToList() ?? new List<int>();
        if (ids.Count == 0)
            return true;

        await using var context = await _contextFactory.CreateDbContextAsync();
        var circuits = await context.LightCircuits
            .Where(c => ids.Contains(c.Id))
            .ToListAsync();

        if (circuits.Count == 0)
            return false;

        bool allSucceeded = true;
        foreach (var circuit in circuits)
        {
            if (!TryParseAddress(circuit.Address, out int address))
            {
                allSucceeded = false;
                continue;
            }

            bool success = await _commService.WriteCircuitControlAsync(address, state);
            if (!success)
            {
                allSucceeded = false;
                continue;
            }

            var telemetry = await ReadTelemetryIfOnAsync(address, state);
            int targetBrightness = state
                ? (circuit.Brightness > 0 ? circuit.Brightness : DefaultBrightness)
                : 0;
            ApplyCircuitState(circuit, state, telemetry.Current, telemetry.Power, targetBrightness);
        }

        await context.SaveChangesAsync();
        return allSucceeded;
    }

    /// <summary>
    /// 批量设置亮度 · 单事务单 SaveChanges
    /// </summary>
    public async Task<bool> BatchSetBrightnessAsync(IEnumerable<int> circuitIds, int brightness)
    {
        var ids = circuitIds?.Distinct().ToList() ?? new List<int>();
        if (ids.Count == 0)
            return true;

        int normalized = Math.Clamp(brightness, 0, 100);

        await using var context = await _contextFactory.CreateDbContextAsync();
        var circuits = await context.LightCircuits
            .Where(c => ids.Contains(c.Id))
            .ToListAsync();

        if (circuits.Count == 0)
            return false;

        bool allSucceeded = true;
        foreach (var circuit in circuits)
        {
            if (!TryParseAddress(circuit.Address, out int address))
            {
                allSucceeded = false;
                continue;
            }

            bool success = await _commService.WriteBrightnessAsync(address, normalized);
            if (!success)
            {
                allSucceeded = false;
                continue;
            }

            bool state = normalized > 0;
            var telemetry = await ReadTelemetryIfOnAsync(address, state);
            ApplyCircuitState(circuit, state, telemetry.Current, telemetry.Power, normalized);
        }

        await context.SaveChangesAsync();
        return allSucceeded;
    }

    public async Task<CircuitStatistics> GetStatisticsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        // SQL 层聚合，不再把所有回路拉回内存
        var stats = await context.LightCircuits
            .GroupBy(c => 1)
            .Select(g => new
            {
                TotalCount = g.Count(),
                OnCount = g.Count(c => c.Status == CircuitStatus.On),
                OffCount = g.Count(c => c.Status == CircuitStatus.Off),
                FaultCount = g.Count(c => c.Status == CircuitStatus.Fault || c.Status == CircuitStatus.Offline),
                TotalPower = g.Sum(c => c.Power)
            })
            .FirstOrDefaultAsync();

        int floorCount = await context.Floors.CountAsync();

        return new CircuitStatistics
        {
            TotalCount = stats?.TotalCount ?? 0,
            OnCount = stats?.OnCount ?? 0,
            OffCount = stats?.OffCount ?? 0,
            FaultCount = stats?.FaultCount ?? 0,
            TotalPower = stats?.TotalPower ?? 0,
            FloorCount = floorCount
        };
    }

    public async Task<FloorLayoutSnapshot?> GetFloorLayoutAsync(int floorId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var floor = await context.Floors
            .AsNoTracking()
            .Include(f => f.Areas)
                .ThenInclude(a => a.LightCircuits)
            .FirstOrDefaultAsync(f => f.Id == floorId);

        return floor == null ? null : MapFloorLayout(floor);
    }

    public async Task<List<FloorLayoutSnapshot>> GetBuildingLayoutAsync()
    {
        var floors = await GetAllFloorsAsync();
        return floors.Select(MapFloorLayout).ToList();
    }

    public async Task UpdateCircuitStatusAsync(int circuitId, bool isOn, float current, float power, int brightness)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var circuit = await context.LightCircuits.FindAsync(circuitId);
        if (circuit == null)
            return;

        ApplyCircuitState(circuit, isOn, current, power, brightness);
        await context.SaveChangesAsync();
    }

    public async Task<bool> SyncCircuitStatusByAddressAsync(int address, bool isOn, float current, float power, int brightness)
    {
        string addressKey = FormatAddress(address);
        await using var context = await _contextFactory.CreateDbContextAsync();
        var circuit = await context.LightCircuits.FirstOrDefaultAsync(c => c.Address == addressKey);
        if (circuit == null)
            return false;

        ApplyCircuitState(circuit, isOn, current, power, brightness);
        await context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 仅在回路开启时并发读取电流/功率，关闭态跳过通信
    /// </summary>
    private async Task<(float Current, float Power)> ReadTelemetryIfOnAsync(int address, bool isOn)
    {
        if (!isOn)
            return (0f, 0f);

        var currentTask = _commService.ReadCurrentAsync(address);
        var powerTask = _commService.ReadPowerAsync(address);
        await Task.WhenAll(currentTask, powerTask);
        return (currentTask.Result, powerTask.Result);
    }

    private static void ApplyCircuitState(LightCircuit circuit, bool isOn, float current, float power, int brightness)
    {
        circuit.IsOn = isOn;
        circuit.Brightness = Math.Clamp(brightness, 0, 100);
        circuit.Current = isOn ? Math.Max(0, current) : 0;
        circuit.Power = isOn ? Math.Max(0, power) : 0;
        circuit.Status = ResolveStatus(isOn, circuit.Current, circuit.Power);
        circuit.LastUpdated = DateTime.Now;

        if (!isOn)
            circuit.LastEnergySampleAt = null;
        else if (!circuit.LastEnergySampleAt.HasValue)
            circuit.LastEnergySampleAt = circuit.LastUpdated;
    }

    private static CircuitStatus ResolveStatus(bool isOn, float current, float power)
    {
        if (!isOn)
            return CircuitStatus.Off;

        if (current < 0 || power < 0)
            return CircuitStatus.Fault;

        return CircuitStatus.On;
    }

    /// <summary>容错解析地址字符串（"0042" → 42）</summary>
    private static bool TryParseAddress(string address, out int parsed)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            parsed = 0;
            return false;
        }
        // TrimStart 0 避免 int.Parse 对前导零的歧义（虽然 int.Parse 支持，但显式更安全）
        return int.TryParse(address, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out parsed);
    }

    private static string FormatAddress(int address) => address.ToString("D4");

    private static FloorLayoutSnapshot MapFloorLayout(Floor floor)
    {
        var areas = floor.Areas
            .OrderBy(a => a.PositionY)
            .ThenBy(a => a.PositionX)
            .ToList();

        var circuits = areas
            .SelectMany(area => area.LightCircuits)
            .OrderBy(c => c.Address)
            .ToList();

        return new FloorLayoutSnapshot
        {
            FloorId = floor.Id,
            FloorName = floor.Name,
            Description = floor.Description,
            Areas = areas.Select(area => new AreaLayoutSnapshot
            {
                AreaId = area.Id,
                Name = area.Name,
                AreaType = area.AreaType,
                PositionX = area.PositionX,
                PositionY = area.PositionY,
                Width = area.Width,
                Height = area.Height
            }).ToList(),
            Circuits = circuits.Select(circuit => new CircuitLayoutSnapshot
            {
                CircuitId = circuit.Id,
                AreaId = circuit.AreaId,
                Name = circuit.Name,
                Address = circuit.Address,
                IsOn = circuit.IsOn,
                Brightness = circuit.Brightness,
                Current = circuit.Current,
                Power = circuit.Power,
                Status = circuit.Status,
                RelativeX = circuit.RelativeX,
                RelativeY = circuit.RelativeY,
                LastUpdated = circuit.LastUpdated
            }).ToList(),
            Summary = new FloorRealtimeSummary
            {
                TotalCount = circuits.Count,
                OnCount = circuits.Count(c => c.IsOn),
                OffCount = circuits.Count(c => !c.IsOn),
                FaultCount = circuits.Count(c => c.Status == CircuitStatus.Fault || c.Status == CircuitStatus.Offline),
                TotalPower = circuits.Sum(c => c.Power)
            }
        };
    }
}
