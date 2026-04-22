using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartBuildingLighting.Core.Enums;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.App.ViewModels;

public partial class ScheduleViewModel : ObservableObject
{
    private readonly IScheduleService _scheduleService;
    private readonly IGroupService _groupService;
    private readonly ISceneModeService _sceneModeService;
    private readonly ILightCircuitService _circuitService;
    private readonly ILogService _logService;
    private readonly IUserService _userService;

    [ObservableProperty] private Schedule? _selectedSchedule;
    [ObservableProperty] private string _editName = "";
    [ObservableProperty] private string _editCron = "0 0 8 * * ?";
    [ObservableProperty] private ScheduleTargetType _selectedTargetType = ScheduleTargetType.Circuit;
    [ObservableProperty] private bool _editTargetState = true;
    [ObservableProperty] private int _editBrightness = 100;
    [ObservableProperty] private int? _selectedCircuitId;
    [ObservableProperty] private int? _selectedGroupId;
    [ObservableProperty] private int? _selectedSceneModeId;
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private bool _isLoading;

    public bool CanManage => _userService.CurrentUser?.Role == UserRole.Admin;
    public Array ScheduleTargetTypes => Enum.GetValues(typeof(ScheduleTargetType));

    public ObservableCollection<Schedule> Schedules { get; } = new();
    public ObservableCollection<LightCircuit> Circuits { get; } = new();
    public ObservableCollection<LightGroup> Groups { get; } = new();
    public ObservableCollection<SceneMode> Modes { get; } = new();
    public ObservableCollection<ScheduleExecutionRecord> ExecutionRecords { get; } = new();

    public ScheduleViewModel(IScheduleService scheduleService, IGroupService groupService, ISceneModeService sceneModeService, ILightCircuitService circuitService, ILogService logService, IUserService userService)
    {
        _scheduleService = scheduleService;
        _groupService = groupService;
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
            int? selectedScheduleId = SelectedSchedule?.Id;
            bool reboundSelection = false;
            Schedules.Clear();
            foreach (var schedule in await _scheduleService.GetAllSchedulesAsync())
                Schedules.Add(schedule);

            Circuits.Clear();
            foreach (var circuit in await _circuitService.GetAllCircuitsAsync())
                Circuits.Add(circuit);

            Groups.Clear();
            foreach (var group in await _groupService.GetAllGroupsAsync())
                Groups.Add(group);

            Modes.Clear();
            foreach (var mode in await _sceneModeService.GetAllModesAsync())
                Modes.Add(mode);

            if (selectedScheduleId.HasValue)
            {
                SelectedSchedule = Schedules.FirstOrDefault(schedule => schedule.Id == selectedScheduleId.Value);
                reboundSelection = SelectedSchedule != null;
            }

            if (SelectedSchedule != null && !reboundSelection)
                await PopulateScheduleAsync(SelectedSchedule.Id);
            else if (!reboundSelection)
                ResetEditor();
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedScheduleChanged(Schedule? value)
    {
        if (value == null)
        {
            ResetEditor();
            ExecutionRecords.Clear();
            return;
        }

        _ = PopulateScheduleAsync(value.Id);
    }

    [RelayCommand]
    private void StartCreate()
    {
        SelectedSchedule = null;
        ResetEditor();
        ExecutionRecords.Clear();
    }

    [RelayCommand]
    private async Task SaveScheduleAsync()
    {
        if (!CanManage)
        {
            Message = "仅管理员可管理定时任务。";
            return;
        }

        var schedule = BuildSchedule();
        if (!_scheduleService.ValidateCronExpression(schedule.CronExpression))
        {
            Message = "Cron 表达式无效。";
            return;
        }

        try
        {
            if (SelectedSchedule == null)
            {
                var created = await _scheduleService.CreateScheduleAsync(schedule);
                await _logService.LogAsync(OperationType.CreateSchedule, $"创建定时任务: {created.Name}", true, created.Name, _userService.CurrentUser?.Id);
                Message = "定时任务已创建。";
            }
            else
            {
                schedule.Id = SelectedSchedule.Id;
                bool success = await _scheduleService.UpdateScheduleAsync(schedule);
                await _logService.LogAsync(OperationType.ModifySchedule, $"更新定时任务: {schedule.Name}", success, schedule.Name, _userService.CurrentUser?.Id);
                Message = success ? "定时任务已更新。" : "定时任务更新失败。";
            }
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }

        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task ToggleScheduleAsync(Schedule schedule)
    {
        bool success = await _scheduleService.ToggleScheduleAsync(schedule.Id, !schedule.IsEnabled);
        Message = success ? "任务状态已切换。" : "任务状态切换失败。";
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task DeleteScheduleAsync(Schedule schedule)
    {
        if (!CanManage)
        {
            Message = "仅管理员可删除定时任务。";
            return;
        }

        bool success = await _scheduleService.DeleteScheduleAsync(schedule.Id);
        await _logService.LogAsync(OperationType.DeleteSchedule, $"删除定时任务: {schedule.Name}", success, schedule.Name, _userService.CurrentUser?.Id);
        if (SelectedSchedule?.Id == schedule.Id)
            SelectedSchedule = null;
        await LoadDataAsync();
        Message = success ? "定时任务已删除。" : "定时任务删除失败。";
    }

    private async Task PopulateScheduleAsync(int scheduleId)
    {
        var schedule = Schedules.FirstOrDefault(item => item.Id == scheduleId);
        if (schedule == null)
            return;

        EditName = schedule.Name;
        EditCron = schedule.CronExpression;
        SelectedTargetType = schedule.TargetType;
        EditTargetState = schedule.TargetState;
        EditBrightness = schedule.TargetBrightness;
        SelectedCircuitId = schedule.CircuitId;
        SelectedGroupId = schedule.GroupId;
        SelectedSceneModeId = schedule.SceneModeId;

        ExecutionRecords.Clear();
        foreach (var record in await _scheduleService.GetExecutionRecordsAsync(scheduleId))
            ExecutionRecords.Add(record);
    }

    private Schedule BuildSchedule()
    {
        return new Schedule
        {
            Name = EditName,
            CronExpression = EditCron,
            TargetType = SelectedTargetType,
            TargetState = EditTargetState,
            TargetBrightness = EditBrightness,
            CircuitId = SelectedTargetType == ScheduleTargetType.Circuit ? SelectedCircuitId : null,
            GroupId = SelectedTargetType == ScheduleTargetType.Group ? SelectedGroupId : null,
            SceneModeId = SelectedTargetType == ScheduleTargetType.SceneMode ? SelectedSceneModeId : null,
            IsEnabled = SelectedSchedule?.IsEnabled ?? true
        };
    }

    private void ResetEditor()
    {
        EditName = string.Empty;
        EditCron = "0 0 8 * * ?";
        SelectedTargetType = ScheduleTargetType.Circuit;
        EditTargetState = true;
        EditBrightness = 100;
        SelectedCircuitId = null;
        SelectedGroupId = null;
        SelectedSceneModeId = null;
    }
}
