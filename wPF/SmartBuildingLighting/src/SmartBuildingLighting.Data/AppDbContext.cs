using Microsoft.EntityFrameworkCore;
using SmartBuildingLighting.Core.Models;

namespace SmartBuildingLighting.Data;

/// <summary>
/// 应用数据库上下文
/// </summary>
public class AppDbContext : DbContext
{
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<LightCircuit> LightCircuits => Set<LightCircuit>();
    public DbSet<LightGroup> LightGroups => Set<LightGroup>();
    public DbSet<LightGroupCircuit> LightGroupCircuits => Set<LightGroupCircuit>();
    public DbSet<SceneMode> SceneModes => Set<SceneMode>();
    public DbSet<SceneModeDetail> SceneModeDetails => Set<SceneModeDetail>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<ScheduleExecutionRecord> ScheduleExecutionRecords => Set<ScheduleExecutionRecord>();
    public DbSet<EnergyRecord> EnergyRecords => Set<EnergyRecord>();
    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();
    public DbSet<User> Users => Set<User>();
    public DbSet<CommunicationProfile> CommunicationProfiles => Set<CommunicationProfile>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置 LightGroupCircuit 多对多关联复合主键
        modelBuilder.Entity<LightGroupCircuit>()
            .HasKey(lgc => new { lgc.GroupId, lgc.CircuitId });

        modelBuilder.Entity<LightGroupCircuit>()
            .HasOne(lgc => lgc.Group)
            .WithMany(g => g.GroupCircuits)
            .HasForeignKey(lgc => lgc.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LightGroupCircuit>()
            .HasOne(lgc => lgc.Circuit)
            .WithMany(c => c.GroupCircuits)
            .HasForeignKey(lgc => lgc.CircuitId)
            .OnDelete(DeleteBehavior.Cascade);

        // 配置 Area -> Floor 关系
        modelBuilder.Entity<Area>()
            .HasOne(a => a.Floor)
            .WithMany(f => f.Areas)
            .HasForeignKey(a => a.FloorId)
            .OnDelete(DeleteBehavior.Cascade);

        // 配置 LightCircuit -> Area 关系
        modelBuilder.Entity<LightCircuit>()
            .HasOne(lc => lc.Area)
            .WithMany(a => a.LightCircuits)
            .HasForeignKey(lc => lc.AreaId)
            .OnDelete(DeleteBehavior.Cascade);

        // 配置 SceneModeDetail 关系
        modelBuilder.Entity<SceneModeDetail>()
            .HasOne(d => d.SceneMode)
            .WithMany(s => s.Details)
            .HasForeignKey(d => d.SceneModeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SceneModeDetail>()
            .HasOne(d => d.Circuit)
            .WithMany(c => c.SceneModeDetails)
            .HasForeignKey(d => d.CircuitId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Schedule>()
            .HasOne(s => s.Group)
            .WithMany()
            .HasForeignKey(s => s.GroupId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Schedule>()
            .HasOne(s => s.SceneMode)
            .WithMany()
            .HasForeignKey(s => s.SceneModeId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ScheduleExecutionRecord>()
            .HasOne(r => r.Schedule)
            .WithMany(s => s.ExecutionRecords)
            .HasForeignKey(r => r.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        // 配置索引
        modelBuilder.Entity<EnergyRecord>()
            .HasIndex(e => e.RecordTime);

        modelBuilder.Entity<EnergyRecord>()
            .HasIndex(e => e.CircuitId);

        modelBuilder.Entity<OperationLog>()
            .HasIndex(o => o.OperationTime);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<CommunicationProfile>()
            .HasIndex(p => p.IsActive);

        modelBuilder.Entity<LightCircuit>()
            .HasIndex(c => c.Address)
            .IsUnique();
    }
}
