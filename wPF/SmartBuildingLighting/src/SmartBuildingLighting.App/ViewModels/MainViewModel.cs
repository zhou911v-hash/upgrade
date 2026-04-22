using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartBuildingLighting.App.Helpers;
using SmartBuildingLighting.Core.Interfaces;

namespace SmartBuildingLighting.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly NavigationService _navigationService;
    private readonly IUserService _userService;
    private readonly DateTime _bootTime = DateTime.Now;
    private readonly DispatcherTimer? _uptimeTimer;
    public event Action? LogoutRequested;

    [ObservableProperty] private string _currentPageTitle = "仪表盘";
    [ObservableProperty] private string _currentPageKey = "Dashboard";
    [ObservableProperty] private string _userName = "";
    [ObservableProperty] private string _userRole = "";
    [ObservableProperty] private int _selectedNavIndex;
    [ObservableProperty] private bool _isAdmin;
    [ObservableProperty] private string _uptimeDisplay = "00:00:00";
    [ObservableProperty] private string _currentTimeDisplay = DateTime.Now.ToString("HH:mm:ss");

    public NavigationService NavigationService => _navigationService;

    public MainViewModel(NavigationService navigationService, IUserService userService)
    {
        _navigationService = navigationService;
        _userService = userService;

        if (_userService.CurrentUser != null)
        {
            UserName = _userService.CurrentUser.DisplayName ?? _userService.CurrentUser.Username;
            IsAdmin = _userService.CurrentUser.Role == Core.Enums.UserRole.Admin;
            UserRole = IsAdmin ? "管理员" : "普通用户";
        }

        // 全局运行时长 + 时钟（标题栏显示）
        if (System.Windows.Application.Current != null)
        {
            _uptimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _uptimeTimer.Tick += (_, _) =>
            {
                var now = DateTime.Now;
                CurrentTimeDisplay = now.ToString("HH:mm:ss");
                var span = now - _bootTime;
                UptimeDisplay = $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
            };
            _uptimeTimer.Start();
        }
    }

    [RelayCommand]
    private void NavigateTo(string page)
    {
        CurrentPageKey = page;
        CurrentPageTitle = page switch
        {
            "Dashboard" => "仪表盘",
            "FloorMonitor" => "楼层监控",
            "ThreeDMonitor" => "三维监控",
            "GroupControl" => "分组控制",
            "SceneMode" => "情景模式",
            "Schedule" => "定时调度",
            "EnergyStats" => "能耗统计",
            "Log" => "操作日志",
            "Settings" => "系统设置",
            _ => page
        };
        _navigationService.NavigateTo(page);
    }

    [RelayCommand]
    private void Logout()
    {
        _userService.Logout();
        LogoutRequested?.Invoke();
    }

    public void NavigateToDefault()
    {
        NavigateTo("Dashboard");
    }
}
