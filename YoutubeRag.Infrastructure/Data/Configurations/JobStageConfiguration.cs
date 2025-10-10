using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Infrastructure.Data.Configurations;

public class JobStageConfiguration : IEntityTypeConfiguration<JobStage>
{
    public void Configure(EntityTypeBuilder<JobStage> builder)
    {
        builder.HasKey(js => js.Id);

        builder.Property(js => js.Id)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(js => js.JobId)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(js => js.StageName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(js => js.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(js => js.Progress)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(js => js.Order)
            .IsRequired();

        builder.Property(js => js.Weight)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(js => js.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(js => js.ErrorDetails)
            .HasColumnType("json");

        builder.Property(js => js.InputData)
            .HasColumnType("json");

        builder.Property(js => js.OutputData)
            .HasColumnType("json");

        builder.Property(js => js.Metadata)
            .HasColumnType("json");

        builder.Property(js => js.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(js => js.MaxRetries)
            .IsRequired()
            .HasDefaultValue(3);

        // Relationships
        builder.HasOne(js => js.Job)
            .WithMany()
            .HasForeignKey(js => js.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(js => js.JobId)
            .HasDatabaseName("IX_JobStages_JobId");

        builder.HasIndex(js => new { js.JobId, js.Order })
            .HasDatabaseName("IX_JobStages_JobId_Order")
            .IsUnique();

        builder.HasIndex(js => js.Status)
            .HasDatabaseName("IX_JobStages_Status");

        builder.HasIndex(js => js.CreatedAt)
            .HasDatabaseName("IX_JobStages_CreatedAt");

        // Table name
        builder.ToTable("JobStages");
    }
}
