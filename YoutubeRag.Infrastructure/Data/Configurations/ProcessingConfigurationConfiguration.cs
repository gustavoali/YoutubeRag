using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Infrastructure.Data.Configurations;

public class ProcessingConfigurationConfiguration : IEntityTypeConfiguration<ProcessingConfiguration>
{
    public void Configure(EntityTypeBuilder<ProcessingConfiguration> builder)
    {
        builder.HasKey(pc => pc.Id);

        builder.Property(pc => pc.Id)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(pc => pc.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pc => pc.Description)
            .HasMaxLength(500);

        builder.Property(pc => pc.UseLocalWhisper)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(pc => pc.UseLocalEmbeddings)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(pc => pc.MaxConcurrentJobs)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(pc => pc.RetryAttempts)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(pc => pc.TimeoutMinutes)
            .IsRequired()
            .HasDefaultValue(30);

        builder.Property(pc => pc.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(pc => pc.WhisperModel)
            .HasMaxLength(50);

        builder.Property(pc => pc.WhisperLanguage)
            .HasMaxLength(10);

        builder.Property(pc => pc.EmbeddingModel)
            .HasMaxLength(100);

        builder.Property(pc => pc.ChunkSize)
            .IsRequired()
            .HasDefaultValue(500);

        builder.Property(pc => pc.ChunkOverlap)
            .IsRequired()
            .HasDefaultValue(50);

        builder.Property(pc => pc.DefaultQueue)
            .HasMaxLength(50);

        builder.Property(pc => pc.Priority)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(pc => pc.AdditionalSettings)
            .HasColumnType("json");

        // Indexes
        builder.HasIndex(pc => pc.Name)
            .HasDatabaseName("IX_ProcessingConfigurations_Name")
            .IsUnique();

        builder.HasIndex(pc => pc.IsActive)
            .HasDatabaseName("IX_ProcessingConfigurations_IsActive");

        builder.HasIndex(pc => pc.CreatedAt)
            .HasDatabaseName("IX_ProcessingConfigurations_CreatedAt");

        // Table name
        builder.ToTable("ProcessingConfigurations");
    }
}
