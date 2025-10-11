using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Infrastructure.Data.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        // Table name
        builder.ToTable("Jobs");

        // Primary key
        builder.HasKey(j => j.Id);

        // Properties
        builder.Property(j => j.Id)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(j => j.Type)
            .IsRequired()
            .HasDefaultValue(JobType.VideoProcessing)
            .HasConversion<string>();

        builder.Property(j => j.Status)
            .IsRequired()
            .HasDefaultValue(JobStatus.Pending)
            .HasConversion<string>();

        builder.Property(j => j.StatusMessage)
            .HasMaxLength(500);

        builder.Property(j => j.Progress)
            .HasDefaultValue(0);

        builder.Property(j => j.CurrentStage)
            .IsRequired()
            .HasDefaultValue(PipelineStage.None)
            .HasConversion<string>();

        builder.Property(j => j.StageProgressJson)
            .HasColumnType("JSON")
            .HasColumnName("StageProgress");

        builder.Property(j => j.Result)
            .HasColumnType("TEXT");

        builder.Property(j => j.ErrorMessage)
            .HasColumnType("TEXT");

        // Enhanced error tracking fields (GAP-2)
        builder.Property(j => j.ErrorStackTrace)
            .HasColumnType("TEXT");

        builder.Property(j => j.ErrorType)
            .HasMaxLength(500);

        builder.Property(j => j.FailedStage)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(j => j.Parameters)
            .HasColumnType("JSON");

        builder.Property(j => j.Metadata)
            .HasColumnType("JSON");

        builder.Property(j => j.RetryCount)
            .HasDefaultValue(0);

        builder.Property(j => j.MaxRetries)
            .HasDefaultValue(3);

        builder.Property(j => j.NextRetryAt);

        builder.Property(j => j.LastFailureCategory)
            .HasMaxLength(100);

        builder.Property(j => j.WorkerId)
            .HasMaxLength(255);

        builder.Property(j => j.HangfireJobId)
            .HasMaxLength(100);

        builder.Property(j => j.UserId)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(j => j.VideoId)
            .HasMaxLength(36);

        builder.Property(j => j.CreatedAt)
            .IsRequired();

        builder.Property(j => j.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(j => j.Status)
            .HasDatabaseName("IX_Jobs_Status");

        builder.HasIndex(j => j.UserId)
            .HasDatabaseName("IX_Jobs_UserId");

        builder.HasIndex(j => j.VideoId)
            .HasDatabaseName("IX_Jobs_VideoId");

        builder.HasIndex(j => new { j.Status, j.WorkerId })
            .HasDatabaseName("IX_Jobs_Status_WorkerId");

        builder.HasIndex(j => new { j.UserId, j.Status })
            .HasDatabaseName("IX_Jobs_UserId_Status");

        builder.HasIndex(j => j.HangfireJobId)
            .HasDatabaseName("IX_Jobs_HangfireJobId")
            .IsUnique()
            .HasFilter("HangfireJobId IS NOT NULL");

        // Relationships
        builder.HasOne(j => j.User)
            .WithMany(u => u.Jobs)
            .HasForeignKey(j => j.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(j => j.Video)
            .WithMany(v => v.Jobs)
            .HasForeignKey(j => j.VideoId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
