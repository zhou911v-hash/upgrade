using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartBuildingLighting.Core.Enums;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.Data;

/// <summary>
/// 数据库种子数据初始化
/// </summary>
public static class SeedData
{
    /// <summary>
    /// 初始化数据库并填充种子数据
    /// </summary>
    public static async Task InitializeAsync(AppDbContext context)
    {
        // 确保数据库已创建
        await context.Database.EnsureCreatedAsync();

        // 如果已有数据则跳过
        if (await context.Floors.AnyAsync())
            return;

        // 创建默认用户
        await SeedUsersAsync(context);

        // 创建默认通信配置
        await SeedCommunicationProfilesAsync(context);

        // 创建楼层、区域、回路
        await SeedBuildingDataAsync(context);

        // 创建预设情景模式
        await SeedSceneModesAsync(context);

        // 创建示例能耗数据
        await SeedEnergyDataAsync(context);

        await context.SaveChangesAsync();
    }

    private static async Task SeedCommunicationProfilesAsync(AppDbContext context)
    {
        var profile = new CommunicationProfile
        {
            Name = "演示模拟器",
            Mode = CommunicationMode.Simulator,
            UseSimulator = true,
            Host = "127.0.0.1",
            Port = 502,
            UnitId = 1,
            CoilBaseAddress = 0,
            BrightnessRegisterBase = 1000,
            CurrentRegisterBase = 2000,
            PowerRegisterBase = 3000,
            TelemetryIntervalSeconds = 30,
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        await context.CommunicationProfiles.AddAsync(profile);
        await context.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(AppDbContext context)
    {
        var users = new List<User>
        {
            new User
            {
                Username = "admin",
                PasswordHash = HashPassword("admin123"),
                Role = UserRole.Admin,
                DisplayName = "系统管理员",
                CreatedAt = DateTime.Now
            },
            new User
            {
                Username = "user",
                PasswordHash = HashPassword("user123"),
                Role = UserRole.User,
                DisplayName = "普通用户",
                CreatedAt = DateTime.Now
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
    }

    private static async Task SeedBuildingDataAsync(AppDbContext context)
    {
        int addressCounter = 1;

        // 定义5层楼结构
        var floorDefinitions = new[]
        {
            new { Number = 1, Name = "1楼 - 大厅层", Description = "一楼大厅、接待区、物业办公室", 
                  Areas = new[] {
                      ("大厅", "大厅", 5.0, 10.0, 55.0, 35.0, 3),
                      ("接待区", "办公室", 65.0, 10.0, 30.0, 35.0, 2),
                      ("走廊", "走廊", 5.0, 50.0, 90.0, 10.0, 4),
                      ("物业办公室", "办公室", 5.0, 65.0, 40.0, 30.0, 3),
                      ("卫生间", "卫生间", 50.0, 65.0, 20.0, 30.0, 2),
                      ("设备间", "设备间", 75.0, 65.0, 20.0, 30.0, 1)
                  }
            },
            new { Number = 2, Name = "2楼 - 办公层A", Description = "二楼开放办公区、小型会议室",
                  Areas = new[] {
                      ("开放办公区A", "办公室", 5.0, 10.0, 45.0, 40.0, 6),
                      ("开放办公区B", "办公室", 55.0, 10.0, 40.0, 40.0, 6),
                      ("小会议室1", "会议室", 5.0, 55.0, 25.0, 40.0, 2),
                      ("小会议室2", "会议室", 35.0, 55.0, 25.0, 40.0, 2),
                      ("走廊", "走廊", 5.0, 50.0, 90.0, 5.0, 3),
                      ("卫生间", "卫生间", 65.0, 55.0, 15.0, 40.0, 2),
                      ("茶水间", "其他", 82.0, 55.0, 13.0, 40.0, 1)
                  }
            },
            new { Number = 3, Name = "3楼 - 办公层B", Description = "三楼主管办公室、中型会议室",
                  Areas = new[] {
                      ("主管办公室1", "办公室", 5.0, 10.0, 25.0, 40.0, 2),
                      ("主管办公室2", "办公室", 35.0, 10.0, 25.0, 40.0, 2),
                      ("中型会议室", "会议室", 65.0, 10.0, 30.0, 40.0, 4),
                      ("开放办公区", "办公室", 5.0, 55.0, 55.0, 40.0, 5),
                      ("走廊", "走廊", 5.0, 50.0, 90.0, 5.0, 3),
                      ("卫生间", "卫生间", 65.0, 55.0, 15.0, 40.0, 2),
                      ("档案室", "其他", 82.0, 55.0, 13.0, 40.0, 1)
                  }
            },
            new { Number = 4, Name = "4楼 - 会议层", Description = "四楼大型会议室、培训室、休息区",
                  Areas = new[] {
                      ("大型会议室", "会议室", 5.0, 10.0, 55.0, 40.0, 8),
                      ("培训室", "会议室", 65.0, 10.0, 30.0, 40.0, 4),
                      ("走廊", "走廊", 5.0, 50.0, 90.0, 5.0, 3),
                      ("休息区", "其他", 5.0, 60.0, 35.0, 35.0, 3),
                      ("卫生间", "卫生间", 45.0, 60.0, 15.0, 35.0, 2),
                      ("储物间", "其他", 65.0, 60.0, 30.0, 35.0, 1)
                  }
            },
            new { Number = 5, Name = "5楼 - 管理层", Description = "五楼总经理办公室、副总办公室、行政部",
                  Areas = new[] {
                      ("总经理办公室", "办公室", 5.0, 10.0, 35.0, 40.0, 4),
                      ("副总办公室", "办公室", 45.0, 10.0, 25.0, 40.0, 3),
                      ("行政部", "办公室", 75.0, 10.0, 20.0, 40.0, 3),
                      ("走廊", "走廊", 5.0, 50.0, 90.0, 5.0, 3),
                      ("VIP会议室", "会议室", 5.0, 60.0, 40.0, 35.0, 4),
                      ("卫生间", "卫生间", 50.0, 60.0, 15.0, 35.0, 2),
                      ("茶水间", "其他", 70.0, 60.0, 25.0, 35.0, 1)
                  }
            }
        };

        foreach (var floorDef in floorDefinitions)
        {
            var floor = new Floor
            {
                Name = floorDef.Name,
                FloorNumber = floorDef.Number,
                Description = floorDef.Description
            };

            context.Floors.Add(floor);
            await context.SaveChangesAsync();

            foreach (var areaDef in floorDef.Areas)
            {
                var area = new Area
                {
                    FloorId = floor.Id,
                    Name = areaDef.Item1,
                    AreaType = areaDef.Item2,
                    PositionX = areaDef.Item3,
                    PositionY = areaDef.Item4,
                    Width = areaDef.Item5,
                    Height = areaDef.Item6
                };

                context.Areas.Add(area);
                await context.SaveChangesAsync();

                int circuitCount = areaDef.Item7;
                var random = new Random(addressCounter);

                for (int i = 1; i <= circuitCount; i++)
                {
                    var circuit = new LightCircuit
                    {
                        AreaId = area.Id,
                        Name = $"{area.Name}-照明{i}",
                        Address = addressCounter.ToString("D4"),
                        Status = CircuitStatus.Off,
                        IsOn = false,
                        Current = 0,
                        Power = 0,
                        Brightness = 0,
                        RelativeX = (i * 100.0 / (circuitCount + 1)),
                        RelativeY = 50.0,
                        LastUpdated = DateTime.Now
                    };

                    context.LightCircuits.Add(circuit);
                    addressCounter++;
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedSceneModesAsync(AppDbContext context)
    {
        var allCircuits = await context.LightCircuits.ToListAsync();

        // 全开模式
        var allOnMode = new SceneMode
        {
            Name = "全开模式",
            Description = "打开所有照明回路，亮度100%",
            ModeType = SceneModeType.Preset,
            IconName = "Sun"
        };
        context.SceneModes.Add(allOnMode);
        await context.SaveChangesAsync();

        foreach (var circuit in allCircuits)
        {
            context.SceneModeDetails.Add(new SceneModeDetail
            {
                SceneModeId = allOnMode.Id,
                CircuitId = circuit.Id,
                TargetState = true,
                TargetBrightness = 100
            });
        }

        // 全关模式
        var allOffMode = new SceneMode
        {
            Name = "全关模式",
            Description = "关闭所有照明回路",
            ModeType = SceneModeType.Preset,
            IconName = "Moon"
        };
        context.SceneModes.Add(allOffMode);
        await context.SaveChangesAsync();

        foreach (var circuit in allCircuits)
        {
            context.SceneModeDetails.Add(new SceneModeDetail
            {
                SceneModeId = allOffMode.Id,
                CircuitId = circuit.Id,
                TargetState = false,
                TargetBrightness = 0
            });
        }

        // 下班模式 — 仅保留走廊和大厅照明
        var offWorkMode = new SceneMode
        {
            Name = "下班模式",
            Description = "仅保留走廊和大厅照明，其余关闭",
            ModeType = SceneModeType.Preset,
            IconName = "Briefcase"
        };
        context.SceneModes.Add(offWorkMode);
        await context.SaveChangesAsync();

        var corridorAndLobbyCircuits = await context.LightCircuits
            .Include(c => c.Area)
            .ToListAsync();

        foreach (var circuit in corridorAndLobbyCircuits)
        {
            bool keepOn = circuit.Area?.AreaType == "走廊" || circuit.Area?.AreaType == "大厅";
            context.SceneModeDetails.Add(new SceneModeDetail
            {
                SceneModeId = offWorkMode.Id,
                CircuitId = circuit.Id,
                TargetState = keepOn,
                TargetBrightness = keepOn ? 60 : 0
            });
        }

        // 节能模式 — 所有灯光调至50%亮度
        var energySaveMode = new SceneMode
        {
            Name = "节能模式",
            Description = "所有照明回路亮度调至50%，降低能耗",
            ModeType = SceneModeType.Preset,
            IconName = "Leaf"
        };
        context.SceneModes.Add(energySaveMode);
        await context.SaveChangesAsync();

        foreach (var circuit in allCircuits)
        {
            context.SceneModeDetails.Add(new SceneModeDetail
            {
                SceneModeId = energySaveMode.Id,
                CircuitId = circuit.Id,
                TargetState = true,
                TargetBrightness = 50
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedEnergyDataAsync(AppDbContext context)
    {
        var circuits = await context.LightCircuits
            .Include(c => c.Area)
            .ToListAsync();
        var random = new Random(42);
        var records = new List<EnergyRecord>();

        // 生成过去30天的模拟能耗数据
        for (int day = 30; day >= 0; day--)
        {
            var date = DateTime.Now.Date.AddDays(-day);
            foreach (var circuit in circuits)
            {
                // 每天生成4条记录（每6小时一条）
                for (int hour = 0; hour < 24; hour += 6)
                {
                    float basePower = circuit.Area?.AreaType switch
                    {
                        "大厅" => 0.8f,
                        "办公室" => 0.5f,
                        "会议室" => 0.6f,
                        "走廊" => 0.3f,
                        "卫生间" => 0.2f,
                        _ => 0.3f
                    };

                    // 白天能耗更高
                    float timeMultiplier = (hour >= 6 && hour < 18) ? 1.5f : 0.3f;
                    // 工作日能耗更高
                    float dayMultiplier = (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday) ? 1.2f : 0.5f;

                    float consumption = basePower * timeMultiplier * dayMultiplier * (0.8f + (float)random.NextDouble() * 0.4f);

                    records.Add(new EnergyRecord
                    {
                        CircuitId = circuit.Id,
                        PowerConsumption = (float)Math.Round(consumption, 3),
                        RecordTime = date.AddHours(hour)
                    });
                }
            }
        }

        await context.EnergyRecords.AddRangeAsync(records);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// SHA256 密码哈希
    /// </summary>
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }
}
