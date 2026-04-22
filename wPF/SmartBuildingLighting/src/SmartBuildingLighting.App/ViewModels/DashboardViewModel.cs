using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly ILightCircuitService _circuitService;
    private readonly IEnergyService _energyService;
    private readonly ILogService _logService;
    private readonly DispatcherTimer? _clockTimer;

    [ObservableProperty] private int _totalCircuits;
    [ObservableProperty] private int _onlineCircuits;
    [ObservableProperty] private int _offlineCircuits;
    [ObservableProperty] private int _floorCount;
    [ObservableProperty] private float _totalPower;
    [ObservableProperty] private float _todayEnergy;
    [ObservableProperty] private float _yesterdayEnergy;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private string _lastUpdateDisplay = DateTime.Now.ToString("HH:mm:ss");
    [ObservableProperty] private string _uptimeDisplay = "00:00:00";
    [ObservableProperty] private IEnumerable<ISeries> _energyTrendSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _energyTrendXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _energyTrendYAxes = Array.Empty<Axis>();
    [ObservableProperty] private IEnumerable<ISeries> _floorEnergySeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _floorEnergyXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _floorEnergyYAxes = Array.Empty<Axis>();
    [ObservableProperty] private PointCollection _powerSparklinePoints = new();
    [ObservableProperty] private PointCollection _energySparklinePoints = new();
    [ObservableProperty] private PointCollection _onlineSparklinePoints = new();
    [ObservableProperty] private string _energyTrendDelta = "";
    [ObservableProperty] private bool _energyTrendIsDown;
    [ObservableProperty] private string _activeRatioDisplay = "—";

    /// <summary>最近 6 条 AI 洞察（规则推断，非真 AI）</summary>
    public ObservableCollection<string> Insights { get; } = new();

    public ObservableCollection<OperationLog> RecentLogs { get; } = new();

    private readonly DateTime _bootTime = DateTime.Now;

    public DashboardViewModel(ILightCircuitService circuitService, IEnergyService energyService, ILogService logService)
    {
        _circuitService = circuitService;
        _energyService = energyService;
        _logService = logService;

        // 实时时钟 + 运行时长 - 每秒更新
        if (System.Windows.Application.Current != null)
        {
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (_, _) =>
            {
                var now = DateTime.Now;
                LastUpdateDisplay = now.ToString("HH:mm:ss");
                var span = now - _bootTime;
                UptimeDisplay = $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
            };
            _clockTimer.Start();
        }
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var stats = await _circuitService.GetStatisticsAsync();
            TotalCircuits = stats.TotalCount;
            OnlineCircuits = stats.OnCount;
            OfflineCircuits = stats.OffCount;
            FloorCount = stats.FloorCount;
            TotalPower = stats.TotalPower;
            TodayEnergy = await _energyService.GetTodayTotalEnergyAsync();

            // 昨日能耗（用于同比箭头）
            var yesterday = DateTime.Now.Date.AddDays(-1);
            var yesterdayEnergyDict = await _energyService.GetDailyEnergyAsync(yesterday, yesterday.AddDays(1).AddTicks(-1));
            YesterdayEnergy = yesterdayEnergyDict.Values.FirstOrDefault();

            ComputeTrendDelta();
            ComputeActiveRatio();

            RecentLogs.Clear();
            foreach (var log in await _logService.GetRecentLogsAsync(8))
                RecentLogs.Add(log);

            var daily = await _energyService.GetDailyEnergyAsync(DateTime.Now.Date.AddDays(-6), DateTime.Now);
            BuildEnergyTrend(daily);
            BuildEnergySparkline(daily);

            var floorEnergy = await _energyService.GetEnergyByFloorAsync(DateTime.Now.Date.AddDays(-30), DateTime.Now);
            BuildFloorEnergy(floorEnergy);

            BuildPowerSparkline();
            BuildOnlineSparkline();
            BuildInsights(stats, daily);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ComputeTrendDelta()
    {
        if (YesterdayEnergy <= 0.01f)
        {
            EnergyTrendDelta = "—";
            EnergyTrendIsDown = false;
            return;
        }
        var delta = (TodayEnergy - YesterdayEnergy) / YesterdayEnergy * 100f;
        EnergyTrendIsDown = delta < 0;
        EnergyTrendDelta = $"{(delta >= 0 ? "+" : "")}{delta:F1}%";
    }

    private void ComputeActiveRatio()
    {
        if (TotalCircuits <= 0)
        {
            ActiveRatioDisplay = "—";
            return;
        }
        ActiveRatioDisplay = $"{(OnlineCircuits * 100.0 / TotalCircuits):F0}%";
    }

    /// <summary>
    /// 实时功率 sparkline（基于当前总功率生成最近 24 点波动曲线 —— 视觉动效，非持久化数据）
    /// </summary>
    private void BuildPowerSparkline()
    {
        var rnd = new Random(TotalCircuits * 31 + (int)TotalPower);
        var pts = new PointCollection();
        const int count = 24;
        for (int i = 0; i < count; i++)
        {
            float ratio = 0.65f + (float)rnd.NextDouble() * 0.35f;
            pts.Add(new Point(i * 4.2, 24 - ratio * 22));
        }
        PowerSparklinePoints = pts;
    }

    private void BuildOnlineSparkline()
    {
        var rnd = new Random(OnlineCircuits * 17 + 3);
        var pts = new PointCollection();
        const int count = 24;
        for (int i = 0; i < count; i++)
        {
            float ratio = 0.7f + (float)rnd.NextDouble() * 0.3f;
            pts.Add(new Point(i * 4.2, 24 - ratio * 22));
        }
        OnlineSparklinePoints = pts;
    }

    private void BuildEnergySparkline(Dictionary<DateTime, float> daily)
    {
        var values = daily.OrderBy(d => d.Key).Select(d => d.Value).ToList();
        if (values.Count == 0)
        {
            EnergySparklinePoints = new PointCollection();
            return;
        }
        float min = values.Min();
        float max = values.Max();
        float range = Math.Max(0.01f, max - min);
        var pts = new PointCollection();
        double step = values.Count <= 1 ? 0 : 100.0 / (values.Count - 1);
        for (int i = 0; i < values.Count; i++)
        {
            double norm = (values[i] - min) / range;
            pts.Add(new Point(i * step, 24 - norm * 22));
        }
        EnergySparklinePoints = pts;
    }

    private void BuildInsights(CircuitStatistics stats, Dictionary<DateTime, float> daily)
    {
        Insights.Clear();
        if (stats.FaultCount > 0)
            Insights.Add($"检测到 {stats.FaultCount} 个故障/离线回路，请优先排查");

        if (YesterdayEnergy > 0 && TodayEnergy > 0)
        {
            var delta = (TodayEnergy - YesterdayEnergy) / YesterdayEnergy * 100f;
            if (delta < -5)
                Insights.Add($"今日能耗较昨日下降 {Math.Abs(delta):F1}%，节能表现良好");
            else if (delta > 10)
                Insights.Add($"今日能耗较昨日上升 {delta:F1}%，建议开启节能模式");
        }

        if (stats.TotalCount > 0)
        {
            double ratio = stats.OnCount * 1.0 / stats.TotalCount;
            if (ratio > 0.85)
                Insights.Add($"当前开启率 {ratio * 100:F0}%，接近高峰值，考虑关闭次要回路");
            else if (ratio < 0.2)
                Insights.Add($"当前开启率 {ratio * 100:F0}%，处于低谷状态");
        }

        var hour = DateTime.Now.Hour;
        if (hour >= 18 && hour < 20)
            Insights.Add("接近下班时间，建议检查并应用「下班模式」");
        else if (hour >= 8 && hour < 10)
            Insights.Add("上班时段，确认主要办公区域回路已就绪");

        // 至少留 2 条，避免面板空洞
        if (Insights.Count == 0)
            Insights.Add("系统运行正常，无异常推断");
        if (Insights.Count < 2)
            Insights.Add($"数据已同步 · 最近 7 日平均能耗 {(daily.Count > 0 ? daily.Values.Average() : 0):F1} kWh/天");
    }

    private void BuildEnergyTrend(Dictionary<DateTime, float> daily)
    {
        var points = daily.OrderBy(item => item.Key).ToList();
        EnergyTrendSeries = new ISeries[]
        {
            new LineSeries<float>
            {
                Values = points.Select(item => item.Value).ToArray(),
                GeometrySize = 10,
                LineSmoothness = 0.6,
                Fill = null,
                Stroke = new SolidColorPaint(new SKColor(0, 229, 255), 3),
                GeometryStroke = new SolidColorPaint(new SKColor(0, 229, 255), 2),
                GeometryFill = new SolidColorPaint(new SKColor(0, 229, 255))
            }
        };

        EnergyTrendXAxes = new[]
        {
            new Axis
            {
                Labels = points.Select(item => item.Key.ToString("MM/dd")).ToArray(),
                LabelsPaint = new SolidColorPaint(new SKColor(138, 169, 201)),
                TextSize = 11
            }
        };

        EnergyTrendYAxes = new[]
        {
            new Axis
            {
                LabelsPaint = new SolidColorPaint(new SKColor(138, 169, 201)),
                TextSize = 11,
                Name = "kWh",
                NamePaint = new SolidColorPaint(new SKColor(0, 229, 255))
            }
        };
    }

    private void BuildFloorEnergy(Dictionary<string, float> floorEnergy)
    {
        var points = floorEnergy.OrderBy(item => item.Key).ToList();
        FloorEnergySeries = new ISeries[]
        {
            new ColumnSeries<float>
            {
                Values = points.Select(item => item.Value).ToArray(),
                Fill = new SolidColorPaint(new SKColor(0, 229, 255, 190)),
                Stroke = null,
                MaxBarWidth = 42
            }
        };

        FloorEnergyXAxes = new[]
        {
            new Axis
            {
                Labels = points.Select(item => item.Key).ToArray(),
                LabelsPaint = new SolidColorPaint(new SKColor(138, 169, 201)),
                TextSize = 11
            }
        };

        FloorEnergyYAxes = new[]
        {
            new Axis
            {
                LabelsPaint = new SolidColorPaint(new SKColor(138, 169, 201)),
                TextSize = 11,
                Name = "kWh",
                NamePaint = new SolidColorPaint(new SKColor(0, 229, 255))
            }
        };
    }
}
