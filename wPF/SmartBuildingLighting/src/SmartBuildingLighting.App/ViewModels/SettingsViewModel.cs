using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartBuildingLighting.Core.Enums;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ICommunicationService _commService;
    private readonly IUserService _userService;
    private readonly ICommunicationProfileService _profileService;

    private int _profileId;

    [ObservableProperty] private string _profileName = "默认通信配置";
    [ObservableProperty] private CommunicationMode _selectedMode = CommunicationMode.Simulator;
    [ObservableProperty] private string _serverHost = "127.0.0.1";
    [ObservableProperty] private int _serverPort = 502;
    [ObservableProperty] private byte _unitId = 1;
    [ObservableProperty] private int _coilBaseAddress;
    [ObservableProperty] private int _brightnessRegisterBase = 1000;
    [ObservableProperty] private int _currentRegisterBase = 2000;
    [ObservableProperty] private int _powerRegisterBase = 3000;
    [ObservableProperty] private int _telemetryIntervalSeconds = 30;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _connectionStatus = "未连接";
    [ObservableProperty] private string _oldPassword = "";
    [ObservableProperty] private string _newPassword = "";
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private bool _isLoading;

    public Array CommunicationModes => Enum.GetValues(typeof(CommunicationMode));

    public bool IsAdmin => _userService.CurrentUser?.Role == UserRole.Admin;

    public SettingsViewModel(ICommunicationService commService, IUserService userService, ICommunicationProfileService profileService)
    {
        _commService = commService;
        _userService = userService;
        _profileService = profileService;
        UpdateConnectionStatus();
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var profile = await _profileService.GetActiveProfileAsync();
            if (profile == null)
                return;

            _profileId = profile.Id;
            ProfileName = profile.Name;
            SelectedMode = profile.Mode;
            ServerHost = profile.Host;
            ServerPort = profile.Port;
            UnitId = profile.UnitId;
            CoilBaseAddress = profile.CoilBaseAddress;
            BrightnessRegisterBase = profile.BrightnessRegisterBase;
            CurrentRegisterBase = profile.CurrentRegisterBase;
            PowerRegisterBase = profile.PowerRegisterBase;
            TelemetryIntervalSeconds = profile.TelemetryIntervalSeconds;
            UpdateConnectionStatus();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (!IsAdmin)
        {
            Message = "仅管理员可修改系统通信配置。";
            return;
        }

        var saved = await _profileService.SaveAsync(BuildProfile());
        _profileId = saved.Id;
        Message = "通信配置已保存。";
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        try
        {
            var profile = await _profileService.SaveAsync(BuildProfile());
            _profileId = profile.Id;
            bool success = await _commService.ConnectAsync(profile);
            UpdateConnectionStatus();
            Message = success ? "通信连接成功。" : "通信连接失败，请检查参数。";
        }
        catch (Exception ex)
        {
            UpdateConnectionStatus();
            Message = $"连接异常: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _commService.DisconnectAsync();
        UpdateConnectionStatus();
        Message = "已断开连接。";
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (!IsAdmin)
        {
            Message = "仅管理员可修改密码。";
            return;
        }

        if (_userService.CurrentUser == null)
        {
            Message = "请先登录。";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            Message = "新密码不能为空。";
            return;
        }

        bool success = await _userService.ChangePasswordAsync(_userService.CurrentUser.Id, OldPassword, NewPassword);
        Message = success ? "密码修改成功。" : "原密码错误。";
        if (success)
        {
            OldPassword = string.Empty;
            NewPassword = string.Empty;
        }
    }

    private CommunicationProfile BuildProfile()
    {
        return new CommunicationProfile
        {
            Id = _profileId,
            Name = string.IsNullOrWhiteSpace(ProfileName) ? "默认通信配置" : ProfileName,
            Mode = SelectedMode,
            UseSimulator = SelectedMode == CommunicationMode.Simulator,
            Host = ServerHost,
            Port = ServerPort,
            UnitId = UnitId,
            CoilBaseAddress = CoilBaseAddress,
            BrightnessRegisterBase = BrightnessRegisterBase,
            CurrentRegisterBase = CurrentRegisterBase,
            PowerRegisterBase = PowerRegisterBase,
            TelemetryIntervalSeconds = Math.Max(TelemetryIntervalSeconds, 15),
            IsActive = true,
            UpdatedAt = DateTime.Now
        };
    }

    private void UpdateConnectionStatus()
    {
        IsConnected = _commService.IsConnected;
        ConnectionStatus = IsConnected ? "已连接" : "未连接";
    }
}
