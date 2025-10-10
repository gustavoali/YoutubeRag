using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;

namespace YoutubeRag.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for UserNotification entity
/// </summary>
public class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        // Table name
        builder.ToTable("UserNotifications");

        // Primary key
        builder.HasKey(n => n.Id);

        // Properties
        builder.Property(n => n.Id)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(n => n.UserId)
            .HasMaxLength(36);  // Nullable for broadcast notifications

        builder.Property(n => n.Type)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(NotificationType.Info);

        builder.Property(n => n.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(n => n.Message)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(n => n.JobId)
            .HasMaxLength(36);

        builder.Property(n => n.VideoId)
            .HasMaxLength(36);

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.ReadAt);

        builder.Property(n => n.MetadataJson)
            .HasColumnType("JSON")
            .HasColumnName("Metadata");

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.Property(n => n.UpdatedAt)
            .IsRequired();

        // Indexes for efficient querying
        builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
            .HasDatabaseName("IX_UserNotifications_UserId_IsRead_CreatedAt");

        builder.HasIndex(n => n.CreatedAt)
            .HasDatabaseName("IX_UserNotifications_CreatedAt");

        builder.HasIndex(n => n.Type)
            .HasDatabaseName("IX_UserNotifications_Type");

        builder.HasIndex(n => n.JobId)
            .HasDatabaseName("IX_UserNotifications_JobId");

        builder.HasIndex(n => n.VideoId)
            .HasDatabaseName("IX_UserNotifications_VideoId");

        // Relationships
        builder.HasOne(n => n.Job)
            .WithMany()
            .HasForeignKey(n => n.JobId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(n => n.Video)
            .WithMany()
            .HasForeignKey(n => n.VideoId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
