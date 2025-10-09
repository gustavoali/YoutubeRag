using Microsoft.EntityFrameworkCore;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Infrastructure.Data.Configurations;

namespace YoutubeRag.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Video> Videos { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<JobStage> JobStages { get; set; }
    public DbSet<TranscriptSegment> TranscriptSegments { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<ProcessingConfiguration> ProcessingConfigurations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the assembly
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new VideoConfiguration());
        modelBuilder.ApplyConfiguration(new JobConfiguration());
        modelBuilder.ApplyConfiguration(new JobStageConfiguration());
        modelBuilder.ApplyConfiguration(new TranscriptSegmentConfiguration());
        modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessingConfigurationConfiguration());

        // Alternative: Apply all configurations automatically
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // CRITICAL FIX (ISSUE-002): Only set timestamps if not already set
                    // This allows callers to set shared timestamps for bulk insert operations
                    // while maintaining automatic timestamp behavior for entities that don't set them
                    if (entry.Entity.CreatedAt == default)
                    {
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                    }

                    if (entry.Entity.UpdatedAt == default)
                    {
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                    }
                    break;

                case EntityState.Modified:
                    // Always update UpdatedAt for modifications
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }
    }
}
