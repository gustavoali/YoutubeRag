using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for DeadLetterJob entity
/// </summary>
public class DeadLetterJobConfiguration : IEntityTypeConfiguration<DeadLetterJob>
{
    public void Configure(EntityTypeBuilder<DeadLetterJob> builder)
    {
        // Table name
        builder.ToTable("DeadLetterJobs");

        // Primary key
        builder.HasKey(dlj => dlj.Id);

        // Properties
        builder.Property(dlj => dlj.Id)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(dlj => dlj.JobId)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(dlj => dlj.FailureReason)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(dlj => dlj.FailureDetails)
            .HasColumnType("TEXT");

        builder.Property(dlj => dlj.OriginalPayload)
            .HasColumnType("JSON");

        builder.Property(dlj => dlj.FailedAt)
            .IsRequired();

        builder.Property(dlj => dlj.AttemptedRetries)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(dlj => dlj.IsRequeued)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(dlj => dlj.RequeuedAt);

        builder.Property(dlj => dlj.RequeuedBy)
            .HasMaxLength(255);

        builder.Property(dlj => dlj.Notes)
            .HasColumnType("TEXT");

        builder.Property(dlj => dlj.CreatedAt)
            .IsRequired();

        builder.Property(dlj => dlj.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(dlj => dlj.JobId)
            .HasDatabaseName("IX_DeadLetterJobs_JobId")
            .IsUnique();

        builder.HasIndex(dlj => dlj.FailureReason)
            .HasDatabaseName("IX_DeadLetterJobs_FailureReason");

        builder.HasIndex(dlj => dlj.FailedAt)
            .HasDatabaseName("IX_DeadLetterJobs_FailedAt");

        builder.HasIndex(dlj => dlj.IsRequeued)
            .HasDatabaseName("IX_DeadLetterJobs_IsRequeued");

        // Relationships
        builder.HasOne(dlj => dlj.Job)
            .WithMany()
            .HasForeignKey(dlj => dlj.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
