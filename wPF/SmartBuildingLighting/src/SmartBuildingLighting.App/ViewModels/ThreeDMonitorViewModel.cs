using System.Collections.ObjectModel;
using System.Windows.Media.Media3D;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartBuildingLighting.Core.Enums;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.App.ViewModels;

public partial class ThreeDMonitorViewModel : ObservableObject
{
    private readonly ILightCircuitService _circuitService;
    private readonly ILogService _logService;
    private readonly IUserService _userService;
    private readonly Dictionary<int, FloorLayoutSnapshot> _layoutCache = new();

    [ObservableProperty] private Floor? _selectedFloor;
    [ObservableProperty] private bool _showAllFloors = true;
    [ObservableProperty] private Circuit3DItem? _selectedCircuit;
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<Floor> Floors { get; } = new();
    public ObservableCollection<Circuit3DItem> VisibleCircuits { get; } = new();
    public IEnumerable<FloorLayoutSnapshot> VisibleLayouts => _layoutCache.Values
        .Where(layout => ShowAllFloors || layout.FloorId == SelectedFloor?.Id)
        .OrderBy(layout => layout.FloorId);

    public ThreeDMonitorViewModel(ILightCircuitService circuitService, ILogService logService, IUserService userService)
    {
        _circuitService = circuitService;
        _logService = logService;
        _userService = userService;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var floors = await _circuitService.GetAllFloorsAsync();
            Floors.Clear();
            foreach (var floor in floors)
                Floors.Add(floor);

            _layoutCache.Clear();
            foreach (var layout in await _circuitService.GetBuildingLayoutAsync())
                _layoutCache[layout.FloorId] = layout;

            if (SelectedFloor == null && Floors.Count > 0)
                SelectedFloor = Floors.First();

            RebuildSceneData();
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedFloorChanged(Floor? value) => RebuildSceneData();

    partial void OnShowAllFloorsChanged(bool value) => RebuildSceneData();

    [RelayCommand]
    private void SelectCircuit(Circuit3DItem item)
    {
        SelectedCircuit = item;
        OnPropertyChanged(nameof(SelectedCircuit));
    }

    [RelayCommand]
    private async Task ToggleCircuitAsync(Circuit3DItem item)
    {
        bool targetState = !item.IsOn;
        bool success = await _circuitService.ToggleCircuitAsync(item.CircuitId, targetState);
        if (!success)
            return;

        await _logService.LogAsync(
            targetState ? OperationType.TurnOn : OperationType.TurnOff,
            $"{(targetState ? "开启" : "关闭")}三维监控回路: {item.Name}",
            success,
            item.Name,
            _userService.CurrentUser?.Id);

        await LoadDataAsync();
        SelectedCircuit = VisibleCircuits.FirstOrDefault(c => c.CircuitId == item.CircuitId);
    }

    [RelayCommand]
    private async Task ApplyBrightnessAsync()
    {
        if (SelectedCircuit == null)
            return;

        bool success = await _circuitService.SetBrightnessAsync(SelectedCircuit.CircuitId, SelectedCircuit.Brightness);
        if (!success)
            return;

        await _logService.LogAsync(OperationType.AdjustBrightness, $"三维监控调整亮度至 {SelectedCircuit.Brightness}%", true, SelectedCircuit.Name, _userService.CurrentUser?.Id);
        await LoadDataAsync();
        SelectedCircuit = VisibleCircuits.FirstOrDefault(c => c.CircuitId == SelectedCircuit.CircuitId);
    }

    private void RebuildSceneData()
    {
        VisibleCircuits.Clear();
        foreach (var layout in VisibleLayouts)
        {
            double floorBaseY = (layout.FloorId - 1) * 14;
            var areasById = layout.Areas.ToDictionary(a => a.AreaId);
            foreach (var circuit in layout.Circuits)
            {
                if (!areasById.TryGetValue(circuit.AreaId, out var area))
                    continue;

                var circuitItem = new Circuit3DItem
                {
                    CircuitId = circuit.CircuitId,
                    Name = circuit.Name,
                    Address = circuit.Address,
                    Brightness = circuit.Brightness,
                    Current = circuit.Current,
                    Power = circuit.Power,
                    IsOn = circuit.IsOn,
                    Status = circuit.Status,
                    FloorName = layout.FloorName,
                    Position = CreateCircuitPoint(area, circuit, floorBaseY)
                };

                VisibleCircuits.Add(circuitItem);
            }
        }

        if (SelectedCircuit != null && VisibleCircuits.All(c => c.CircuitId != SelectedCircuit.CircuitId))
            SelectedCircuit = null;
        OnPropertyChanged(nameof(VisibleLayouts));
    }

    private static Point3D CreateAreaCenter(AreaLayoutSnapshot area, double floorBaseY)
    {
        double x = (area.PositionX + area.Width / 2d - 50d) * 0.38;
        double y = (area.PositionY + area.Height / 2d - 50d) * 0.38;
        return new Point3D(x, y, floorBaseY);
    }

    private static Point3D CreateCircuitPoint(AreaLayoutSnapshot area, CircuitLayoutSnapshot circuit, double floorBaseY)
    {
        double worldX = (area.PositionX + area.Width * circuit.RelativeX / 100d - 50d) * 0.38;
        double worldY = (area.PositionY + area.Height * circuit.RelativeY / 100d - 50d) * 0.38;
        double worldZ = floorBaseY + (circuit.IsOn ? 1.6 : 1.0);
        return new Point3D(worldX, worldY, worldZ);
    }
}

public partial class Circuit3DItem : ObservableObject
{
    [ObservableProperty] private int _circuitId;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private bool _isOn;
    [ObservableProperty] private int _brightness;
    [ObservableProperty] private float _current;
    [ObservableProperty] private float _power;
    [ObservableProperty] private CircuitStatus _status;
    [ObservableProperty] private string _floorName = string.Empty;
    [ObservableProperty] private Point3D _position;
}
