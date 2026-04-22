using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartBuildingLighting.Core.Enums;
using SmartBuildingLighting.Core.Interfaces;
using SmartBuildingLighting.Core.Models;
using SmartBuildingLighting.Data;
using SmartBuildingLighting.Services;
using SmartBuildingLighting.Services.Communication;
using Xunit;

namespace SmartBuildingLighting.Tests.Integration;

public class ServiceIntegrationTests
{
    [Fact]
    public async Task SeedData_InitializesExpectedBaseline()
    {
        await using var harness = await TestHarness.CreateAsync();

        await using var scope = harness.Provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(5, await context.Floors.CountAsync());
        Assert.Equal(2, await context.Users.CountAsync());
        Assert.Equal(1, await context.CommunicationProfiles.CountAsync());
        Assert.True(await context.LightCircuits.CountAsync() > 0);
        Assert.True(await context.SceneModes.CountAsync() >= 4);
    }

    [Fact]
    public async Task ToggleCircuit_UpdatesCircuitAndFloorLayout()
    {
        await using var harness = await TestHarness.CreateAsync();
        await harness.ConnectSimulatorAsync();

        await using var scope = harness.Provider.CreateAsyncScope();
        var circuitService = scope.ServiceProvider.GetRequiredService<ILightCircuitService>();

        var firstCircuit = (await circuitService.GetAllCircuitsAsync()).First();
        bool success = await circuitService.ToggleCircuitAsync(firstCircuit.Id, true);

        Assert.True(success);

        var refreshed = await circuitService.GetCircuitByIdAsync(firstCircuit.Id);
        Assert.NotNull(refreshed);
        Assert.True(refreshed!.IsOn);
        Assert.True(refreshed.Power > 0);

        var layout = await circuitService.GetFloorLayoutAsync(refreshed.Area!.FloorId);
        Assert.NotNull(layout);
        Assert.Contains(layout!.Circuits, circuit => circuit.CircuitId == firstCircuit.Id && circuit.IsOn);
    }

    [Fact]
    public async Task BatchControl_TurnsOnAllCircuitsInSingleTransaction()
    {
        await using var harness = await TestHarness.CreateAsync();
        await harness.ConnectSimulatorAsync();

        await using var scope = harness.Provider.CreateAsyncScope();
        var circuitService = scope.ServiceProvider.GetRequiredService<ILightCircuitService>();

        var circuits = (await circuitService.GetAllCircuitsAsync()).Take(8).ToList();
        var ids = circuits.Select(c => c.Id).ToList();

        bool allOk = await circuitService.BatchControlAsync(ids, true);
        Assert.True(allOk);

        foreach (var id in ids)
        {
            var refreshed = await circuitService.GetCircuitByIdAsync(id);
            Assert.NotNull(refreshed);
            Assert.True(refreshed!.IsOn);
            Assert.Equal(100, refreshed.Brightness);
        }

        // 关回去后功率归零
        bool allOff = await circuitService.BatchControlAsync(ids, false);
        Assert.True(allOff);
        foreach (var id in ids)
        {
            var refreshed = await circuitService.GetCircuitByIdAsync(id);
            Assert.NotNull(refreshed);
            Assert.False(refreshed!.IsOn);
            Assert.Equal(0, refreshed.Power);
        }
    }

    [Fact]
    public async Task UserService_LoginLockout_BlocksAfterRepeatedFailures()
    {
        await using var harness = await TestHarness.CreateAsync();

        await using var scope = harness.Provider.CreateAsyncScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        // 连续 5 次错误密码必须触发锁定
        for (int i = 0; i < 5; i++)
            Assert.Null(await userService.LoginAsync("admin", "wrong-password"));

        // 此时即使密码正确也不能登录
        Assert.Null(await userService.LoginAsync("admin", "admin123"));
    }

    [Fact]
    public async Task ExecuteSchedule_CreatesExecutionRecordAndControlsCircuit()
    {
        await using var harness = await TestHarness.CreateAsync();
        await harness.ConnectSimulatorAsync();

        await using var scope = harness.Provider.CreateAsyncScope();
        var circuitService = scope.ServiceProvider.GetRequiredService<ILightCircuitService>();
        var scheduleService = scope.ServiceProvider.GetRequiredService<IScheduleService>();

        var circuit = (await circuitService.GetAllCircuitsAsync()).First();
        var schedule = await scheduleService.CreateScheduleAsync(new Schedule
        {
            Name = "测试回路定时",
            CronExpression = "0 0 8 * * ?",
            TargetType = ScheduleTargetType.Circuit,
            CircuitId = circuit.Id,
            TargetState = true,
            TargetBrightness = 80,
            IsEnabled = true
        });

        bool executed = await scheduleService.ExecuteScheduleAsync(schedule.Id);
        Assert.True(executed);

        var refreshedCircuit = await circuitService.GetCircuitByIdAsync(circuit.Id);
        Assert.NotNull(refreshedCircuit);
        Assert.True(refreshedCircuit!.IsOn);
        Assert.Equal(80, refreshedCircuit.Brightness);

        var records = await scheduleService.GetExecutionRecordsAsync(schedule.Id);
        Assert.NotEmpty(records);
        Assert.True(records[0].IsSuccess);
    }
}

internal sealed class TestHarness : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private TestHarness(ServiceProvider provider, SqliteConnection connection)
    {
        Provider = provider;
        _connection = connection;
    }

    public ServiceProvider Provider { get; }

    public static async Task<TestHarness> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connection));
        services.AddSingleton<SimulatorService>();
        services.AddSingleton<ModbusTcpCommunicationService>();
        services.AddSingleton<ICommunicationService, CommunicationService>();
        services.AddTransient<ILightCircuitService, LightCircuitService>();
        services.AddTransient<ISceneModeService, SceneModeService>();
        services.AddTransient<IScheduleService, ScheduleService>();
        services.AddTransient<IEnergyService, EnergyService>();
        services.AddTransient<ILogService, LogService>();
        services.AddSingleton<IUserService, UserService>();
        services.AddTransient<IGroupService, GroupService>();
        services.AddTransient<ICommunicationProfileService, CommunicationProfileService>();

        var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SeedData.InitializeAsync(context);

        return new TestHarness(provider, connection);
    }

    public async Task ConnectSimulatorAsync()
    {
        await using var scope = Provider.CreateAsyncScope();
        var communication = scope.ServiceProvider.GetRequiredService<ICommunicationService>();
        var profiles = scope.ServiceProvider.GetRequiredService<ICommunicationProfileService>();
        var profile = await profiles.GetActiveProfileAsync();
        Assert.NotNull(profile);
        await communication.ConnectAsync(profile!);
    }

    public async ValueTask DisposeAsync()
    {
        await Provider.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
