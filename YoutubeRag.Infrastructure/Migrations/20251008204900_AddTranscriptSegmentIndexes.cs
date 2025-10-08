using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeRag.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTranscriptSegmentIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add composite index on VideoId and SegmentIndex for efficient ordering and filtering
            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSegments_VideoId_SegmentIndex",
                table: "TranscriptSegments",
                columns: new[] { "VideoId", "SegmentIndex" });

            // Add index on CreatedAt for time-based queries and cleanup operations
            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSegments_CreatedAt",
                table: "TranscriptSegments",
                column: "CreatedAt");

            // Add index on StartTime for timeline-based queries
            migrationBuilder.CreateIndex(
                name: "IX_TranscriptSegments_StartTime",
                table: "TranscriptSegments",
                column: "StartTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TranscriptSegments_VideoId_SegmentIndex",
                table: "TranscriptSegments");

            migrationBuilder.DropIndex(
                name: "IX_TranscriptSegments_CreatedAt",
                table: "TranscriptSegments");

            migrationBuilder.DropIndex(
                name: "IX_TranscriptSegments_StartTime",
                table: "TranscriptSegments");
        }
    }
}
