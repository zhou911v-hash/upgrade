using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;
using SmartBuildingLighting.Data;

namespace SmartBuildingLighting.Services;

public class EnergyService : IEnergyService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private const double MinimumHoursForSample = 1d / 3600d;

    public EnergyService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task RecordEnergyAsync(int circuitId, float powerConsumption)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.EnergyRecords.Add(new EnergyRecord
        {
            CircuitId = circuitId,
            PowerConsumption = powerConsumption,
            RecordTime = DateTime.Now
        });
        await context.SaveChangesAsync();
    }

    public async Task<List<EnergyRecord>> GetEnergyRecordsAsync(DateTime startTime, DateTime endTime)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.EnergyRecords
            .AsNoTracking()
            .Include(e => e.Circuit)
            .Where(e => e.RecordTime >= startTime && e.RecordTime <= endTime)
            .OrderBy(e => e.RecordTime)
            .ToListAsync();
    }

    /// <summary>
    /// 按日聚合能耗 · SQL 层 GroupBy 避免把全量记录加载到内存
    /// </summary>
    public async Task<Dictionary<DateTime, float>> GetDailyEnergyAsync(DateTime startDate, DateTime endDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var aggregated = await context.EnergyRecords
            .AsNoTracking()
            .Where(e => e.RecordTime >= startDate && e.RecordTime <= endDate)
            .GroupBy(e => e.RecordTime.Date)
            .Select(g => new { Day = g.Key, Total = g.Sum(e => e.PowerConsumption) })
            .OrderBy(x => x.Day)
            .ToListAsync();

        return aggregated.ToDictionary(x => x.Day, x => x.Total);
    }

    /// <summary>
    /// 按楼层聚合能耗 · SQL 层 GroupBy
    /// </summary>
    public async Task<Dictionary<string, float>> GetEnergyByFloorAsync(DateTime startDate, DateTime endDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var aggregated = await context.EnergyRecords
            .AsNoTracking()
            .Where(e => e.RecordTime >= startDate && e.RecordTime <= endDate
                        && e.Circuit != null && e.Circuit.Area != null && e.Circuit.Area.Floor != null)
            .GroupBy(e => e.Circuit!.Area!.Floor!.Name)
            .Select(g => new { Floor = g.Key, Total = g.Sum(e => e.PowerConsumption) })
            .OrderBy(x => x.Floor)
            .ToListAsync();

        return aggregated.ToDictionary(x => x.Floor, x => x.Total);
    }

    /// <summary>
    /// 回路能耗 TOP N · SQL 层聚合 + 排序 + Take，避免全量加载
    /// </summary>
    public async Task<List<(string CircuitName, float TotalEnergy)>> GetTopEnergyCircuitsAsync(int topN, DateTime startDate, DateTime endDate)
    {
        if (topN <= 0)
            return new List<(string, float)>();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var aggregated = await context.EnergyRecords
            .AsNoTracking()
            .Where(e => e.RecordTime >= startDate && e.RecordTime <= endDate && e.Circuit != null)
            .GroupBy(e => e.Circuit!.Name)
            .Select(g => new { Name = g.Key, Total = g.Sum(e => e.PowerConsumption) })
            .OrderByDescending(x => x.Total)
            .Take(topN)
            .ToListAsync();

        return aggregated.Select(x => (x.Name, x.Total)).ToList();
    }

    public async Task<float> GetTodayTotalEnergyAsync()
    {
        var today = DateTime.Now.Date;
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.EnergyRecords
            .AsNoTracking()
            .Where(e => e.RecordTime >= today)
            .SumAsync(e => e.PowerConsumption);
    }

    public async Task<string> ExportToCsvAsync(DateTime startDate, DateTime endDate)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var records = await context.EnergyRecords
            .AsNoTracking()
            .Include(e => e.Circuit)
                .ThenInclude(c => c!.Area)
                    .ThenInclude(a => a!.Floor)
            .Where(e => e.RecordTime >= startDate && e.RecordTime <= endDate)
            .OrderBy(e => e.RecordTime)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("记录ID,回路名称,所属区域,所属楼层,能耗(kWh),记录时间");
        foreach (var r in records)
        {
            // CSV 字段用双引号包裹并对内部双引号转义，避免回路名称含逗号污染列
            sb.AppendLine(string.Join(",",
                r.Id,
                EscapeCsv(r.Circuit?.Name),
                EscapeCsv(r.Circuit?.Area?.Name),
                EscapeCsv(r.Circuit?.Area?.Floor?.Name),
                r.PowerConsumption.ToString("F3"),
                r.RecordTime.ToString("yyyy-MM-dd HH:mm:ss")));
        }

        var filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            $"能耗报表_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv");
        // UTF-8 with BOM 便于 Excel 正确识别中文
        await File.WriteAllTextAsync(filePath, sb.ToString(), new UTF8Encoding(true));
        return filePath;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    public async Task SampleAsync(IEnumerable<LightCircuit> circuits, DateTime sampledAt)
    {
        var sourceList = circuits?.ToList() ?? new List<LightCircuit>();
        if (sourceList.Count == 0)
            return;

        var sourceIds = sourceList.Select(c => c.Id).Distinct().ToArray();

        await using var context = await _contextFactory.CreateDbContextAsync();
        var trackedCircuits = await context.LightCircuits
            .Where(c => sourceIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        var energyRecords = new List<EnergyRecord>();

        foreach (var sourceCircuit in sourceList.Where(c => c.IsOn && c.Power > 0))
        {
            if (!trackedCircuits.TryGetValue(sourceCircuit.Id, out var circuit))
                continue;

            if (!circuit.LastEnergySampleAt.HasValue)
            {
                circuit.LastEnergySampleAt = sampledAt;
                continue;
            }

            var hours = (sampledAt - circuit.LastEnergySampleAt.Value).TotalHours;
            if (hours <= MinimumHoursForSample)
                continue;

            var consumption = (float)Math.Round((circuit.Power / 1000f) * hours, 4);
            if (consumption <= 0)
                continue;

            energyRecords.Add(new EnergyRecord
            {
                CircuitId = circuit.Id,
                PowerConsumption = consumption,
                RecordTime = sampledAt
            });

            circuit.LastEnergySampleAt = sampledAt;
        }

        if (energyRecords.Count > 0)
            await context.EnergyRecords.AddRangeAsync(energyRecords);

        await context.SaveChangesAsync();
    }
}
