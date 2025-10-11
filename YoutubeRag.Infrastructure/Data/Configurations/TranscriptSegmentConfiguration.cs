using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using YoutubeRag.Domain.Entities;

namespace YoutubeRag.Infrastructure.Data.Configurations;

public class TranscriptSegmentConfiguration : IEntityTypeConfiguration<TranscriptSegment>
{
    public void Configure(EntityTypeBuilder<TranscriptSegment> builder)
    {
        // Table name
        builder.ToTable("TranscriptSegments");

        // Primary key
        builder.HasKey(ts => ts.Id);

        // Properties
        builder.Property(ts => ts.Id)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(ts => ts.VideoId)
            .HasMaxLength(36)
            .IsRequired();

        builder.Property(ts => ts.Text)
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Property(ts => ts.StartTime)
            .IsRequired();

        builder.Property(ts => ts.EndTime)
            .IsRequired();

        builder.Property(ts => ts.SegmentIndex)
            .IsRequired();

        builder.Property(ts => ts.EmbeddingVector)
            .HasColumnType("TEXT");

        builder.Property(ts => ts.Confidence)
            .HasPrecision(5, 4); // Precision for confidence scores (0.0000 to 1.0000)

        builder.Property(ts => ts.Language)
            .HasMaxLength(10);

        builder.Property(ts => ts.Speaker)
            .HasMaxLength(100);

        // Computed property - ignore for EF
        builder.Ignore(ts => ts.HasEmbedding);

        builder.Property(ts => ts.CreatedAt)
            .IsRequired();

        builder.Property(ts => ts.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(ts => ts.VideoId)
            .HasDatabaseName("IX_TranscriptSegments_VideoId");

        builder.HasIndex(ts => new { ts.VideoId, ts.SegmentIndex })
            .IsUnique()
            .HasDatabaseName("IX_TranscriptSegments_VideoId_SegmentIndex");

        builder.HasIndex(ts => new { ts.VideoId, ts.StartTime })
            .HasDatabaseName("IX_TranscriptSegments_VideoId_StartTime");

        // Relationships
        builder.HasOne(ts => ts.Video)
            .WithMany(v => v.TranscriptSegments)
            .HasForeignKey(ts => ts.VideoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
