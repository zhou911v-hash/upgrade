using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartBuildingLighting.Core.Enums;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.App.ViewModels;

public partial class GroupControlViewModel : ObservableObject
{
    private readonly IGroupService _groupService;
    private readonly ILightCircuitService _circuitService;
    private readonly ILogService _logService;
    private readonly IUserService _userService;

    [ObservableProperty] private LightGroup? _selectedGroup;
    [ObservableProperty] private string _editGroupName = "";
    [ObservableProperty] private string _editGroupDesc = "";
    [ObservableProperty] private int _groupBrightness = 100;
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private bool _isLoading;

    public bool CanManage => _userService.CurrentUser?.Role == UserRole.Admin;

    public ObservableCollection<LightGroup> Groups { get; } = new();
    public ObservableCollection<CircuitSelectionItem> CircuitOptions { get; } = new();

    public GroupControlViewModel(IGroupService groupService, ILightCircuitService circuitService, ILogService logService, IUserService userService)
    {
        _groupService = groupService;
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
            int? selectedGroupId = SelectedGroup?.Id;
            bool reboundSelection = false;
            Groups.Clear();
            foreach (var group in await _groupService.GetAllGroupsAsync())
                Groups.Add(group);

            var circuits = await _circuitService.GetAllCircuitsAsync();
            CircuitOptions.Clear();
            foreach (var circuit in circuits)
            {
                CircuitOptions.Add(new CircuitSelectionItem
                {
                    CircuitId = circuit.Id,
                    DisplayName = $"{circuit.Area?.Floor?.Name} / {circuit.Area?.Name} / {circuit.Name}",
                    IsSelected = false
                });
            }

            if (selectedGroupId.HasValue)
            {
                SelectedGroup = Groups.FirstOrDefault(group => group.Id == selectedGroupId.Value);
                reboundSelection = SelectedGroup != null;
            }

            if (SelectedGroup == null && !reboundSelection)
                ResetEditor();
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedGroupChanged(LightGroup? value)
    {
        if (value == null)
        {
            ResetEditor();
            return;
        }

        EditGroupName = value.Name;
        EditGroupDesc = value.Description ?? string.Empty;
        foreach (var option in CircuitOptions)
            option.IsSelected = value.GroupCircuits.Any(gc => gc.CircuitId == option.CircuitId);
    }

    [RelayCommand]
    private void StartCreate()
    {
        SelectedGroup = null;
        ResetEditor();
    }

    [RelayCommand]
    private void LoadGroup(LightGroup group)
    {
        SelectedGroup = group;
    }

    [RelayCommand]
    private async Task SaveGroupAsync()
    {
        if (!CanManage)
        {
            Message = "仅管理员可管理分组。";
            return;
        }

        var circuitIds = CircuitOptions.Where(option => option.IsSelected).Select(option => option.CircuitId).ToList();
        if (!circuitIds.Any() || string.IsNullOrWhiteSpace(EditGroupName))
        {
            Message = "请填写分组名称并至少选择一个回路。";
            return;
        }

        if (SelectedGroup == null)
        {
            var created = await _groupService.CreateGroupAsync(EditGroupName, EditGroupDesc, circuitIds);
            await _logService.LogAsync(OperationType.GroupControl, $"创建分组: {created.Name}", true, created.Name, _userService.CurrentUser?.Id);
            Message = "分组已创建。";
        }
        else
        {
            bool success = await _groupService.UpdateGroupAsync(SelectedGroup.Id, EditGroupName, EditGroupDesc, circuitIds);
            await _logService.LogAsync(OperationType.UpdateGroup, $"更新分组: {EditGroupName}", success, EditGroupName, _userService.CurrentUser?.Id);
            Message = success ? "分组已更新。" : "分组更新失败。";
        }

        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task DeleteGroupAsync(LightGroup group)
    {
        if (!CanManage)
        {
            Message = "仅管理员可删除分组。";
            return;
        }

        bool success = await _groupService.DeleteGroupAsync(group.Id);
        await _logService.LogAsync(OperationType.GroupControl, $"删除分组: {group.Name}", success, group.Name, _userService.CurrentUser?.Id);
        if (SelectedGroup?.Id == group.Id)
            SelectedGroup = null;
        await LoadDataAsync();
        Message = success ? "分组已删除。" : "分组删除失败。";
    }

    [RelayCommand]
    private async Task ControlGroupAsync((int groupId, bool state) param)
    {
        bool success = await _groupService.ControlGroupAsync(param.groupId, param.state);
        var group = Groups.FirstOrDefault(g => g.Id == param.groupId);
        await _logService.LogAsync(OperationType.GroupControl, $"{(param.state ? "开启" : "关闭")}分组: {group?.Name}", success, group?.Name, _userService.CurrentUser?.Id);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task SetGroupBrightnessAsync(int groupId)
    {
        bool success = await _groupService.SetGroupBrightnessAsync(groupId, GroupBrightness);
        await _logService.LogAsync(OperationType.AdjustBrightness, $"设置分组亮度为 {GroupBrightness}%", success, Groups.FirstOrDefault(g => g.Id == groupId)?.Name, _userService.CurrentUser?.Id);
        Message = success ? "分组亮度已应用。" : "分组亮度应用失败。";
        await LoadDataAsync();
    }

    private void ResetEditor()
    {
        EditGroupName = string.Empty;
        EditGroupDesc = string.Empty;
        foreach (var option in CircuitOptions)
            option.IsSelected = false;
    }
}

public partial class CircuitSelectionItem : ObservableObject
{
    [ObservableProperty] private int _circuitId;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private bool _isSelected;
}
