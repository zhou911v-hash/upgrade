using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SmartBuildingLighting.Core.Interfaces;

namespace SmartBuildingLighting.App.ViewModels;

public partial class EnergyStatsViewModel : ObservableObject
{
    private readonly IEnergyService _energyService;

    [ObservableProperty] private DateTime _startDate = DateTime.Now.Date.AddDays(-30);
    [ObservableProperty] private DateTime _endDate = DateTime.Now;
    [ObservableProperty] private float _totalEnergy;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _exportPath = "";
    [ObservableProperty] private IEnumerable<ISeries> _dailySeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _dailyXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _dailyYAxes = Array.Empty<Axis>();
    [ObservableProperty] private IEnumerable<ISeries> _floorSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] _floorXAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] _floorYAxes = Array.Empty<Axis>();

    public ObservableCollection<CircuitRanking> TopCircuits { get; } = new();

    public EnergyStatsViewModel(IEnergyService energyService)
    {
        _energyService = energyService;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var daily = await _energyService.GetDailyEnergyAsync(StartDate, EndDate);
            float total = 0;
            var dailyPoints = daily.OrderBy(item => item.Key).ToList();
            foreach (var item in dailyPoints)
                total += item.Value;

            TotalEnergy = total;
            DailySeries = new ISeries[]
            {
                new LineSeries<float>
                {
                    Values = dailyPoints.Select(item => item.Value).ToArray(),
                    Fill = new SolidColorPaint(new SKColor(0, 229, 255, 40)),
                    Stroke = new SolidColorPaint(new SKColor(0, 229, 255), 3),
                    LineSmoothness = 0.6,
                    GeometrySize = 8,
                    GeometryStroke = new SolidColorPaint(new SKColor(0, 229, 255), 2),
                    GeometryFill = new SolidColorPaint(new SKColor(0, 229, 255))
                }
            };
            DailyXAxes = new[]
            {
                new Axis
                {
                    Labels = dailyPoints.Select(item => item.Key.ToString("MM/dd")).ToArray(),
                    LabelsPaint = new SolidColorPaint(new SKColor(138, 169, 201)),
                    TextSize = 11
                }
            };
            DailyYAxes = new[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(new SKColor(138, 169, 201)),
                    Name = "kWh",
                    NamePaint = new SolidColorPaint(new SKColor(0, 229, 255)),
                    TextSize = 11
                }
            };

            var floorData = await _energyService.GetEnergyByFloorAsync(StartDate, EndDate);
            var floors = floorData.OrderBy(item => item.Key).ToList();
            FloorSeries = new ISeries[]
            {
                new ColumnSeries<float>
                {
                    Values = floors.Select(item => item.Value).ToArray(),
                    Fill = new SolidColorPaint(new SKColor(0, 229, 255, 180)),
                    Stroke = null,
                    MaxBarWidth = 42
                }
            };
            FloorXAxes = new[]
            {
                new Axis
                {
                    Labels = floors.Select(item => item.Key).ToArray(),
                    LabelsPaint = new SolidColorPaint(new SKColor(138, 169, 201)),
                    TextSize = 11
                }
            };
            FloorYAxes = new[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(new SKColor(138, 169, 201)),
                    Name = "kWh",
                    NamePaint = new SolidColorPaint(new SKColor(0, 229, 255)),
                    TextSize = 11
                }
            };

            TopCircuits.Clear();
            int rank = 1;
            foreach (var (name, energy) in await _energyService.GetTopEnergyCircuitsAsync(10, StartDate, EndDate))
                TopCircuits.Add(new CircuitRanking { Rank = rank++, Name = name, Energy = energy });
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        ExportPath = await _energyService.ExportToCsvAsync(StartDate, EndDate);
    }
}

public class CircuitRanking
{
    public int Rank { get; set; }
    public string Name { get; set; } = "";
    public float Energy { get; set; }
}
