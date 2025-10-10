using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YoutubeRag.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobErrorTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new fields to Jobs table for enhanced error tracking (GAP-2)
            migrationBuilder.AddColumn<string>(
                name: "ErrorStackTrace",
                table: "Jobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorType",
                table: "Jobs",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailedStage",
                table: "Jobs",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErrorStackTrace",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "ErrorType",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "FailedStage",
                table: "Jobs");
        }
    }
}
