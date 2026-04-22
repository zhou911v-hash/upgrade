using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartBuildingLighting.Core.Enums;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.App.ViewModels;

public partial class FloorMonitorViewModel : ObservableObject
{
    private const double BoardWidth = 920;
    private const double BoardHeight = 560;

    private readonly ILightCircuitService _circuitService;
    private readonly ILogService _logService;
    private readonly IUserService _userService;

    [ObservableProperty] private Floor? _selectedFloor;
    [ObservableProperty] private FloorCircuitItem? _selectedCircuit;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private int _floorOnCount;
    [ObservableProperty] private int _floorOffCount;
    [ObservableProperty] private float _floorTotalPower;
    [ObservableProperty] private int _floorFaultCount;

    public double CanvasWidth => BoardWidth;
    public double CanvasHeight => BoardHeight;

    public ObservableCollection<Floor> Floors { get; } = new();
    public ObservableCollection<FloorAreaItem> FloorAreas { get; } = new();
    public ObservableCollection<FloorCircuitItem> FloorCircuits { get; } = new();

    public FloorMonitorViewModel(ILightCircuitService circuitService, ILogService logService, IUserService userService)
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
            int? selectedFloorId = SelectedFloor?.Id;
            bool reboundSelection = false;
            var floors = await _circuitService.GetAllFloorsAsync();
            Floors.Clear();
            foreach (var floor in floors)
                Floors.Add(floor);

            if (selectedFloorId.HasValue)
            {
                SelectedFloor = Floors.FirstOrDefault(floor => floor.Id == selectedFloorId.Value);
                reboundSelection = SelectedFloor != null;
            }

            if (SelectedFloor == null && Floors.Count > 0)
                SelectedFloor = Floors.First();
            else if (SelectedFloor != null && !reboundSelection)
                await LoadFloorLayoutAsync(SelectedFloor.Id);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedFloorChanged(Floor? value)
    {
        if (value != null)
            _ = LoadFloorLayoutAsync(value.Id);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (SelectedFloor != null)
            await LoadFloorLayoutAsync(SelectedFloor.Id);
    }

    [RelayCommand]
    private void SelectCircuit(FloorCircuitItem circuit)
    {
        SelectedCircuit = circuit;
        foreach (var area in FloorAreas)
            area.IsHighlighted = area.AreaId == circuit.AreaId;
    }

    [RelayCommand]
    private async Task ToggleCircuitAsync(FloorCircuitItem circuit)
    {
        bool targetState = !circuit.IsOn;
        bool success = await _circuitService.ToggleCircuitAsync(circuit.CircuitId, targetState);
        if (!success)
            return;

        await _logService.LogAsync(
            targetState ? OperationType.TurnOn : OperationType.TurnOff,
            $"{(targetState ? "开启" : "关闭")}照明回路: {circuit.Name}",
            targetState,
            circuit.Name,
            _userService.CurrentUser?.Id);

        if (SelectedFloor != null)
            await LoadFloorLayoutAsync(SelectedFloor.Id);
    }

    [RelayCommand]
    private async Task ApplyBrightnessAsync()
    {
        if (SelectedCircuit == null)
            return;

        bool success = await _circuitService.SetBrightnessAsync(SelectedCircuit.CircuitId, SelectedCircuit.Brightness);
        if (!success)
            return;

        await _logService.LogAsync(
            OperationType.AdjustBrightness,
            $"调节亮度至 {SelectedCircuit.Brightness}%",
            true,
            SelectedCircuit.Name,
            _userService.CurrentUser?.Id);

        if (SelectedFloor != null)
            await LoadFloorLayoutAsync(SelectedFloor.Id);
    }

    [RelayCommand]
    private async Task TurnAllOnAsync()
    {
        bool success = await _circuitService.BatchSetBrightnessAsync(FloorCircuits.Select(c => c.CircuitId), 100);
        await _logService.LogAsync(OperationType.TurnOn, $"全开 {SelectedFloor?.Name} 所有照明", success, SelectedFloor?.Name, _userService.CurrentUser?.Id);
        if (SelectedFloor != null)
            await LoadFloorLayoutAsync(SelectedFloor.Id);
    }

    [RelayCommand]
    private async Task TurnAllOffAsync()
    {
        bool success = await _circuitService.BatchControlAsync(FloorCircuits.Select(c => c.CircuitId), false);
        await _logService.LogAsync(OperationType.TurnOff, $"全关 {SelectedFloor?.Name} 所有照明", success, SelectedFloor?.Name, _userService.CurrentUser?.Id);
        if (SelectedFloor != null)
            await LoadFloorLayoutAsync(SelectedFloor.Id);
    }

    private async Task LoadFloorLayoutAsync(int floorId)
    {
        var snapshot = await _circuitService.GetFloorLayoutAsync(floorId);
        if (snapshot == null)
            return;

        var areas = snapshot.Areas
            .Select(area => new FloorAreaItem
            {
                AreaId = area.AreaId,
                Name = area.Name,
                AreaType = area.AreaType,
                X = area.PositionX * BoardWidth / 100d,
                Y = area.PositionY * BoardHeight / 100d,
                Width = area.Width * BoardWidth / 100d,
                Height = area.Height * BoardHeight / 100d
            })
            .ToDictionary(area => area.AreaId);

        FloorAreas.Clear();
        foreach (var area in areas.Values)
            FloorAreas.Add(area);

        FloorCircuits.Clear();
        foreach (var circuit in snapshot.Circuits)
        {
            if (!areas.TryGetValue(circuit.AreaId, out var area))
                continue;

            FloorCircuits.Add(new FloorCircuitItem
            {
                CircuitId = circuit.CircuitId,
                AreaId = circuit.AreaId,
                Name = circuit.Name,
                Address = circuit.Address,
                Status = circuit.Status,
                IsOn = circuit.IsOn,
                Brightness = circuit.Brightness,
                Current = circuit.Current,
                Power = circuit.Power,
                LastUpdated = circuit.LastUpdated,
                X = area.X + area.Width * circuit.RelativeX / 100d - 18,
                Y = area.Y + area.Height * circuit.RelativeY / 100d - 18
            });
        }

        if (SelectedCircuit != null)
        {
            SelectedCircuit = FloorCircuits.FirstOrDefault(c => c.CircuitId == SelectedCircuit.CircuitId);
            if (SelectedCircuit != null)
                SelectCircuit(SelectedCircuit);
        }
        else
        {
            foreach (var area in FloorAreas)
                area.IsHighlighted = false;
        }

        FloorOnCount = snapshot.Summary.OnCount;
        FloorOffCount = snapshot.Summary.OffCount;
        FloorFaultCount = snapshot.Summary.FaultCount;
        FloorTotalPower = snapshot.Summary.TotalPower;
    }
}

public partial class FloorAreaItem : ObservableObject
{
    [ObservableProperty] private int _areaId;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _areaType = string.Empty;
    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;
    [ObservableProperty] private double _width;
    [ObservableProperty] private double _height;
    [ObservableProperty] private bool _isHighlighted;
}

public partial class FloorCircuitItem : ObservableObject
{
    [ObservableProperty] private int _circuitId;
    [ObservableProperty] private int _areaId;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _address = string.Empty;
    [ObservableProperty] private bool _isOn;
    [ObservableProperty] private int _brightness;
    [ObservableProperty] private float _current;
    [ObservableProperty] private float _power;
    [ObservableProperty] private CircuitStatus _status;
    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;
    [ObservableProperty] private DateTime _lastUpdated;
}
