using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeRag.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeadLetterQueueAndPipelineStages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new fields to Jobs table for pipeline stages and retry policies
            migrationBuilder.AddColumn<string>(
                name: "CurrentStage",
                table: "Jobs",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "StageProgress",
                table: "Jobs",
                type: "JSON",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                table: "Jobs",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastFailureCategory",
                table: "Jobs",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            // Create DeadLetterJobs table
            migrationBuilder.CreateTable(
                name: "DeadLetterJobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    JobId = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: false),
                    FailureReason = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    FailureDetails = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalPayload = table.Column<string>(type: "JSON", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AttemptedRetries = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsRequeued = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    RequeuedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RequeuedBy = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadLetterJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeadLetterJobs_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes on DeadLetterJobs table
            migrationBuilder.CreateIndex(
                name: "IX_DeadLetterJobs_JobId",
                table: "DeadLetterJobs",
                column: "JobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeadLetterJobs_FailureReason",
                table: "DeadLetterJobs",
                column: "FailureReason");

            migrationBuilder.CreateIndex(
                name: "IX_DeadLetterJobs_FailedAt",
                table: "DeadLetterJobs",
                column: "FailedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DeadLetterJobs_IsRequeued",
                table: "DeadLetterJobs",
                column: "IsRequeued");

            // Create index on Jobs.NextRetryAt for efficient retry queries
            migrationBuilder.CreateIndex(
                name: "IX_Jobs_NextRetryAt",
                table: "Jobs",
                column: "NextRetryAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop DeadLetterJobs table
            migrationBuilder.DropTable(
                name: "DeadLetterJobs");

            // Drop new indexes on Jobs table
            migrationBuilder.DropIndex(
                name: "IX_Jobs_NextRetryAt",
                table: "Jobs");

            // Remove new columns from Jobs table
            migrationBuilder.DropColumn(
                name: "CurrentStage",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "StageProgress",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "LastFailureCategory",
                table: "Jobs");
        }
    }
}
