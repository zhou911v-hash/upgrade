using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartBuildingLighting.Core.Enums;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.App.ViewModels;

public partial class SceneModeViewModel : ObservableObject
{
    private readonly ISceneModeService _sceneModeService;
    private readonly ILightCircuitService _circuitService;
    private readonly ILogService _logService;
    private readonly IUserService _userService;

    [ObservableProperty] private SceneMode? _selectedMode;
    [ObservableProperty] private bool _isApplying;
    [ObservableProperty] private string _modeName = "";
    [ObservableProperty] private string _modeDescription = "";
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private bool _isLoading;

    public bool CanManage => _userService.CurrentUser?.Role == UserRole.Admin;

    public ObservableCollection<SceneMode> Modes { get; } = new();
    public ObservableCollection<CircuitModeSelectionItem> CircuitOptions { get; } = new();

    public SceneModeViewModel(ISceneModeService sceneModeService, ILightCircuitService circuitService, ILogService logService, IUserService userService)
    {
        _sceneModeService = sceneModeService;
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
            int? selectedModeId = SelectedMode?.Id;
            bool reboundSelection = false;
            Modes.Clear();
            foreach (var mode in await _sceneModeService.GetAllModesAsync())
                Modes.Add(mode);

            var circuits = await _circuitService.GetAllCircuitsAsync();
            CircuitOptions.Clear();
            foreach (var circuit in circuits)
            {
                CircuitOptions.Add(new CircuitModeSelectionItem
                {
                    CircuitId = circuit.Id,
                    DisplayName = $"{circuit.Area?.Floor?.Name} / {circuit.Area?.Name} / {circuit.Name}",
                    TargetState = circuit.IsOn,
                    TargetBrightness = circuit.Brightness > 0 ? circuit.Brightness : 100
                });
            }

            if (selectedModeId.HasValue)
            {
                SelectedMode = Modes.FirstOrDefault(mode => mode.Id == selectedModeId.Value);
                reboundSelection = SelectedMode != null;
            }

            if (SelectedMode != null && !reboundSelection)
                await PopulateModeAsync(SelectedMode.Id);
            else if (!reboundSelection)
                ResetEditor();
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedModeChanged(SceneMode? value)
    {
        if (value == null)
        {
            ResetEditor();
            return;
        }

        _ = PopulateModeAsync(value.Id);
    }

    [RelayCommand]
    private void StartCreate()
    {
        SelectedMode = null;
        ResetEditor();
    }

    [RelayCommand]
    private void SelectMode(SceneMode mode)
    {
        SelectedMode = mode;
    }

    [RelayCommand]
    private async Task ApplyModeAsync(SceneMode mode)
    {
        IsApplying = true;
        try
        {
            bool success = await _sceneModeService.ApplyModeAsync(mode.Id);
            await _logService.LogAsync(OperationType.ApplySceneMode, $"应用情景模式: {mode.Name}", success, mode.Name, _userService.CurrentUser?.Id);
            Message = success ? "情景模式已应用。" : "情景模式应用失败。";
        }
        finally
        {
            IsApplying = false;
        }
    }

    [RelayCommand]
    private async Task SaveModeAsync()
    {
        if (!CanManage)
        {
            Message = "仅管理员可管理情景模式。";
            return;
        }

        var selectedCircuits = CircuitOptions
            .Where(option => option.IsSelected)
            .Select(option => new SceneModeDetail
            {
                CircuitId = option.CircuitId,
                TargetState = option.TargetState,
                TargetBrightness = option.TargetState ? Math.Max(option.TargetBrightness, 1) : 0
            })
            .ToList();

        if (string.IsNullOrWhiteSpace(ModeName) || selectedCircuits.Count == 0)
        {
            Message = "请填写模式名称并选择至少一个回路。";
            return;
        }

        if (SelectedMode == null)
        {
            var mode = new SceneMode
            {
                Name = ModeName,
                Description = ModeDescription,
                ModeType = SceneModeType.Custom,
                IconName = "Custom",
                Details = selectedCircuits
            };
            await _sceneModeService.CreateModeAsync(mode);
            await _logService.LogAsync(OperationType.CreateSceneMode, $"创建情景模式: {mode.Name}", true, mode.Name, _userService.CurrentUser?.Id);
            Message = "情景模式已创建。";
        }
        else
        {
            if (SelectedMode.ModeType == SceneModeType.Preset)
            {
                Message = "预设模式不可修改，请新建自定义模式。";
                return;
            }

            SelectedMode.Name = ModeName;
            SelectedMode.Description = ModeDescription;
            SelectedMode.Details = selectedCircuits;
            bool success = await _sceneModeService.UpdateModeAsync(SelectedMode);
            await _logService.LogAsync(OperationType.UpdateSceneMode, $"更新情景模式: {SelectedMode.Name}", success, SelectedMode.Name, _userService.CurrentUser?.Id);
            Message = success ? "情景模式已更新。" : "情景模式更新失败。";
        }

        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task DeleteModeAsync(SceneMode mode)
    {
        if (!CanManage)
        {
            Message = "仅管理员可删除情景模式。";
            return;
        }

        if (mode.ModeType == SceneModeType.Preset)
        {
            Message = "预设模式不可删除。";
            return;
        }

        bool success = await _sceneModeService.DeleteModeAsync(mode.Id);
        await _logService.LogAsync(OperationType.DeleteSceneMode, $"删除情景模式: {mode.Name}", success, mode.Name, _userService.CurrentUser?.Id);
        if (SelectedMode?.Id == mode.Id)
            SelectedMode = null;
        await LoadDataAsync();
        Message = success ? "情景模式已删除。" : "情景模式删除失败。";
    }

    private async Task PopulateModeAsync(int modeId)
    {
        var mode = await _sceneModeService.GetModeByIdAsync(modeId);
        if (mode == null)
            return;

        ModeName = mode.Name;
        ModeDescription = mode.Description ?? string.Empty;

        foreach (var option in CircuitOptions)
        {
            var detail = mode.Details.FirstOrDefault(item => item.CircuitId == option.CircuitId);
            option.IsSelected = detail != null;
            option.TargetState = detail?.TargetState ?? false;
            option.TargetBrightness = detail?.TargetBrightness ?? 100;
        }
    }

    private void ResetEditor()
    {
        ModeName = string.Empty;
        ModeDescription = string.Empty;
        foreach (var option in CircuitOptions)
        {
            option.IsSelected = false;
            option.TargetState = true;
            option.TargetBrightness = 100;
        }
    }
}

public partial class CircuitModeSelectionItem : ObservableObject
{
    [ObservableProperty] private int _circuitId;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _targetState = true;
    [ObservableProperty] private int _targetBrightness = 100;
}
