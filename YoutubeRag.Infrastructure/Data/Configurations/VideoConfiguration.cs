using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Infrastructure.Data.Configurations;

public class VideoConfiguration : IEntityTypeConfiguration<Video>
{
    public void Configure(EntityTypeBuilder<Video> builder)
    {
        // Table name
        builder.ToTable("Videos");

        // Primary key
        builder.HasKey(v => v.Id);

        // Properties
        builder.Property(v => v.Id)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(v => v.Title)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(v => v.Description)
            .HasColumnType("TEXT");

        builder.Property(v => v.YouTubeId)
            .HasMaxLength(50);

        builder.Property(v => v.Url)
            .HasMaxLength(500);

        builder.Property(v => v.OriginalUrl)
            .HasMaxLength(500);

        builder.Property(v => v.ThumbnailUrl)
            .HasMaxLength(500);

        builder.Property(v => v.Duration)
            .HasConversion(
                v => v.HasValue ? v.Value.TotalSeconds : (double?)null,
                v => v.HasValue ? TimeSpan.FromSeconds(v.Value) : null);

        builder.Property(v => v.PublishedAt);

        builder.Property(v => v.ChannelId)
            .HasMaxLength(100);

        builder.Property(v => v.ChannelTitle)
            .HasMaxLength(255);

        builder.Property(v => v.CategoryId)
            .HasMaxLength(50);

        builder.Property(v => v.Tags)
            .HasColumnType("json");

        builder.Property(v => v.Status)
            .IsRequired()
            .HasDefaultValue(VideoStatus.Pending)
            .HasConversion<string>();

        builder.Property(v => v.FilePath)
            .HasMaxLength(500);

        builder.Property(v => v.AudioPath)
            .HasMaxLength(500);

        builder.Property(v => v.ProcessingLog)
            .HasColumnType("TEXT");

        builder.Property(v => v.ErrorMessage)
            .HasColumnType("TEXT");

        builder.Property(v => v.ProcessingProgress)
            .HasDefaultValue(0);

        builder.Property(v => v.Metadata)
            .HasColumnType("JSON");

        builder.Property(v => v.Language)
            .HasMaxLength(10);

        builder.Property(v => v.TranscriptionStatus)
            .IsRequired()
            .HasDefaultValue(TranscriptionStatus.NotStarted)
            .HasConversion<string>();

        builder.Property(v => v.TranscribedAt);

        builder.Property(v => v.EmbeddingStatus)
            .IsRequired()
            .HasDefaultValue(EmbeddingStatus.None)
            .HasConversion<string>();

        builder.Property(v => v.EmbeddedAt);

        builder.Property(v => v.EmbeddingProgress)
            .HasDefaultValue(0);

        builder.Property(v => v.UserId)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(v => v.CreatedAt)
            .IsRequired();

        builder.Property(v => v.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(v => v.YouTubeId)
            .HasDatabaseName("IX_Videos_YouTubeId");

        builder.HasIndex(v => v.Status)
            .HasDatabaseName("IX_Videos_Status");

        builder.HasIndex(v => v.TranscriptionStatus)
            .HasDatabaseName("IX_Videos_TranscriptionStatus");

        builder.HasIndex(v => v.EmbeddingStatus)
            .HasDatabaseName("IX_Videos_EmbeddingStatus");

        builder.HasIndex(v => v.UserId)
            .HasDatabaseName("IX_Videos_UserId");

        builder.HasIndex(v => new { v.UserId, v.Status })
            .HasDatabaseName("IX_Videos_UserId_Status");

        // Relationships
        builder.HasOne(v => v.User)
            .WithMany(u => u.Videos)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Jobs)
            .WithOne(j => j.Video)
            .HasForeignKey(j => j.VideoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(v => v.TranscriptSegments)
            .WithOne(ts => ts.Video)
            .HasForeignKey(ts => ts.VideoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
