using Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "App_Data"));
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Scan> Scans => Set<Scan>();
    public DbSet<TechnologyFinding> TechnologyFindings => Set<TechnologyFinding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Scans)
            .WithOne(s => s.Project)
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Scan>()
            .HasMany(s => s.TechnologyFindings)
            .WithOne(tf => tf.Scan)
            .HasForeignKey(tf => tf.ScanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Project>()
            .Property(p => p.LastScannedAt)
            .HasConversion(
                v => v,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        modelBuilder.Entity<Scan>()
            .Property(s => s.StartedAt)
            .HasConversion(
                v => v,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        modelBuilder.Entity<Scan>()
            .Property(s => s.FinishedAt)
            .HasConversion(
                v => v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);
    }
}
