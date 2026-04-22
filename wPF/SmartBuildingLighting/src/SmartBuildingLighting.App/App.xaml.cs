using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartBuildingLighting.App.Helpers;
using SmartBuildingLighting.App.ViewModels;
using SmartBuildingLighting.App.Views;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Data;
using SmartBuildingLighting.Services;
using SmartBuildingLighting.Services.Communication;

namespace SmartBuildingLighting.App;

public partial class App : Application
{
    private ServiceProvider _serviceProvider = null!;
    private ISystemRuntimeService? _runtimeService;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 配置依赖注入
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        // 初始化数据库
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await SeedData.InitializeAsync(context);
        }

        _runtimeService = _serviceProvider.GetRequiredService<ISystemRuntimeService>();
        await _runtimeService.StartAsync();

        ShowLoginWindow();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // 数据库
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite("Data Source=SmartBuildingLighting.db"));
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite("Data Source=SmartBuildingLighting.db"));

        // 通信后端与运行时
        services.AddSingleton<SimulatorService>();
        services.AddSingleton<ModbusTcpCommunicationService>();
        services.AddSingleton<ICommunicationService, CommunicationService>();
        services.AddSingleton<ISystemRuntimeService, SystemRuntimeService>();

        // 业务服务
        services.AddTransient<ILightCircuitService, LightCircuitService>();
        services.AddTransient<ISceneModeService, SceneModeService>();
        services.AddTransient<IScheduleService, ScheduleService>();
        services.AddTransient<IEnergyService, EnergyService>();
        services.AddTransient<ILogService, LogService>();
        services.AddSingleton<IUserService, UserService>();
        services.AddTransient<IGroupService, GroupService>();
        services.AddTransient<ICommunicationProfileService, CommunicationProfileService>();

        // 导航服务（单例）
        services.AddSingleton<NavigationService>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<FloorMonitorViewModel>();
        services.AddTransient<ThreeDMonitorViewModel>();
        services.AddTransient<GroupControlViewModel>();
        services.AddTransient<SceneModeViewModel>();
        services.AddTransient<ScheduleViewModel>();
        services.AddTransient<EnergyStatsViewModel>();
        services.AddTransient<LogViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Views
        services.AddTransient<LoginView>();
        services.AddTransient<MainWindow>();
    }

    public void ShowMainWindow()
    {
        // 注册导航
        var navService = _serviceProvider.GetRequiredService<NavigationService>();
        navService.Register("Dashboard", () => _serviceProvider.GetRequiredService<DashboardViewModel>());
        navService.Register("FloorMonitor", () => _serviceProvider.GetRequiredService<FloorMonitorViewModel>());
        navService.Register("ThreeDMonitor", () => _serviceProvider.GetRequiredService<ThreeDMonitorViewModel>());
        navService.Register("GroupControl", () => _serviceProvider.GetRequiredService<GroupControlViewModel>());
        navService.Register("SceneMode", () => _serviceProvider.GetRequiredService<SceneModeViewModel>());
        navService.Register("Schedule", () => _serviceProvider.GetRequiredService<ScheduleViewModel>());
        navService.Register("EnergyStats", () => _serviceProvider.GetRequiredService<EnergyStatsViewModel>());
        navService.Register("Log", () => _serviceProvider.GetRequiredService<LogViewModel>());
        navService.Register("Settings", () => _serviceProvider.GetRequiredService<SettingsViewModel>());

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    public void ShowLoginWindow()
    {
        var loginView = _serviceProvider.GetRequiredService<LoginView>();
        loginView.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_runtimeService != null)
            await _runtimeService.StopAsync();

        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
