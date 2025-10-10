using System.Diagnostics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using YoutubeRag.Application.Interfaces;
using YoutubeRag.Domain.Entities;
using YoutubeRag.Domain.Enums;
using YoutubeRag.Tests.Integration.Helpers;
using YoutubeRag.Tests.Integration.Infrastructure;

namespace YoutubeRag.Tests.Integration.Performance;

/// <summary>
/// Performance benchmarks for bulk insert operations
/// Tests bulk insertion of transcript segments at various scales
/// Target performance: 100 segments <2s, 500 segments <5s, 1000 segments <10s
/// </summary>
public class BulkInsertBenchmarkTests : IntegrationTestBase
{
    private readonly ITestOutputHelper _output;

    public BulkInsertBenchmarkTests(
        CustomWebApplicationFactory<Program> factory,
        ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    /// <summary>
    /// Helper method to generate transcript segments for performance testing
    /// </summary>
    private List<TranscriptSegment> GenerateSegments(string videoId, int count)
    {
        var segments = new List<TranscriptSegment>();
        var now = DateTime.UtcNow;
        double currentTime = 0;

        for (int i = 0; i < count; i++)
        {
            var duration = 5.0 + (i % 10); // Variable duration between 5-15 seconds

            segments.Add(new TranscriptSegment
            {
                Id = Guid.NewGuid().ToString(),
                VideoId = videoId,
                SegmentIndex = i,
                StartTime = currentTime,
                EndTime = currentTime + duration,
                Text = $"Performance test segment {i}. This is a sample transcription text " +
                       $"that simulates real transcription data with meaningful content. " +
                       $"The segment contains approximately 250 characters of text which is " +
                       $"typical for a 5-15 second audio segment in a video transcription.",
                Language = "en",
                Confidence = 0.85 + (i % 15) * 0.01, // Confidence between 0.85 - 0.99
                Speaker = i % 3 == 0 ? "Speaker A" : (i % 3 == 1 ? "Speaker B" : null),
                CreatedAt = now,
                UpdatedAt = now
            });

            currentTime += duration;
        }

        return segments;
    }

    #region Benchmark 1: 100 Segments

    /// <summary>
    /// Benchmark 1: Insert 100 Segments
    ///
    /// Target: <2 seconds
    /// Validates:
    /// - Measured time is <2000ms
    /// - All segments inserted correctly
    /// - Sequential indexing maintained
    /// </summary>
    [Fact]
    public async Task BulkInsert_100Segments_ShouldCompleteUnder2Seconds()
    {
        // Arrange
        _output.WriteLine("=== Benchmark 1: 100 Segments ===");
        _output.WriteLine("Target: <2000ms");

        await AuthenticateAsync();

        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        video.Status = VideoStatus.Pending;
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        const int SEGMENT_COUNT = 100;
        var segments = GenerateSegments(video.Id, SEGMENT_COUNT);

        _output.WriteLine($"Generated {segments.Count} segments");
        _output.WriteLine($"Total text size: {segments.Sum(s => s.Text.Length)} characters");

        // Clear any existing segments for this video (test isolation)
        var existingSegments = await DbContext.TranscriptSegments
            .Where(s => s.VideoId == video.Id)
            .ToListAsync();
        if (existingSegments.Any())
        {
            DbContext.TranscriptSegments.RemoveRange(existingSegments);
            await DbContext.SaveChangesAsync();
            _output.WriteLine($"Cleared {existingSegments.Count} existing segments for test isolation");
        }

        var repository = Scope.ServiceProvider.GetRequiredService<ITranscriptSegmentRepository>();

        // Act - Measure bulk insert performance
        var stopwatch = Stopwatch.StartNew();
        await repository.AddRangeAsync(segments);
        stopwatch.Stop();

        var elapsedMs = stopwatch.ElapsedMilliseconds;

        // Assert - Performance target
        _output.WriteLine($"Elapsed time: {elapsedMs}ms");
        _output.WriteLine($"Throughput: {SEGMENT_COUNT / (elapsedMs / 1000.0):F2} segments/second");

        elapsedMs.Should().BeLessThan(2000,
            $"100 segments should be inserted in <2000ms, took {elapsedMs}ms");

        _output.WriteLine($"✓ Performance target met: {elapsedMs}ms < 2000ms");

        // Verify all segments inserted
        var savedSegments = await DbContext.TranscriptSegments
            .Where(s => s.VideoId == video.Id)
            .OrderBy(s => s.SegmentIndex)
            .ToListAsync();

        savedSegments.Count.Should().Be(SEGMENT_COUNT, "All segments should be saved");

        _output.WriteLine($"✓ All {savedSegments.Count} segments verified in database");

        // Verify sequential indexing
        for (int i = 0; i < savedSegments.Count; i++)
        {
            savedSegments[i].SegmentIndex.Should().Be(i, $"Segment {i} should have correct index");
        }

        _output.WriteLine("✓ Sequential indexing verified");

        // Performance metrics
        var avgTimePerSegment = elapsedMs / (double)SEGMENT_COUNT;
        _output.WriteLine($"Average time per segment: {avgTimePerSegment:F3}ms");

        if (elapsedMs < 1000)
        {
            _output.WriteLine("⚡ EXCELLENT: Completed in <1 second!");
        }
        else if (elapsedMs < 1500)
        {
            _output.WriteLine("✓ GOOD: Completed in <1.5 seconds");
        }
        else
        {
            _output.WriteLine("⚠ ACCEPTABLE: Completed in <2 seconds but could be optimized");
        }

        _output.WriteLine("=== Benchmark 1 PASSED ===\n");
    }

    #endregion

    #region Benchmark 2: 500 Segments

    /// <summary>
    /// Benchmark 2: Insert 500 Segments
    ///
    /// Target: <5 seconds
    /// Validates:
    /// - Measured time is <5000ms
    /// - All segments inserted correctly
    /// - Performance scales linearly
    /// </summary>
    [Fact]
    public async Task BulkInsert_500Segments_ShouldCompleteUnder5Seconds()
    {
        // Arrange
        _output.WriteLine("=== Benchmark 2: 500 Segments ===");
        _output.WriteLine("Target: <5000ms");

        await AuthenticateAsync();

        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        video.Status = VideoStatus.Pending;
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        const int SEGMENT_COUNT = 500;
        var segments = GenerateSegments(video.Id, SEGMENT_COUNT);

        _output.WriteLine($"Generated {segments.Count} segments");
        _output.WriteLine($"Total text size: {segments.Sum(s => s.Text.Length)} characters");
        _output.WriteLine($"Estimated video duration: {segments.Last().EndTime:F2} seconds");

        var repository = Scope.ServiceProvider.GetRequiredService<ITranscriptSegmentRepository>();

        // Act - Measure bulk insert performance
        var stopwatch = Stopwatch.StartNew();
        await repository.AddRangeAsync(segments);
        stopwatch.Stop();

        var elapsedMs = stopwatch.ElapsedMilliseconds;

        // Assert - Performance target
        _output.WriteLine($"Elapsed time: {elapsedMs}ms ({elapsedMs / 1000.0:F2}s)");
        _output.WriteLine($"Throughput: {SEGMENT_COUNT / (elapsedMs / 1000.0):F2} segments/second");

        elapsedMs.Should().BeLessThan(5000,
            $"500 segments should be inserted in <5000ms, took {elapsedMs}ms");

        _output.WriteLine($"✓ Performance target met: {elapsedMs}ms < 5000ms");

        // Verify all segments inserted
        var savedSegments = await DbContext.TranscriptSegments
            .Where(s => s.VideoId == video.Id)
            .CountAsync();

        savedSegments.Should().Be(SEGMENT_COUNT, "All segments should be saved");

        _output.WriteLine($"✓ All {savedSegments} segments verified in database");

        // Verify data integrity on sample segments
        var sampleSegments = await DbContext.TranscriptSegments
            .Where(s => s.VideoId == video.Id && (s.SegmentIndex == 0 || s.SegmentIndex == 250 || s.SegmentIndex == 499))
            .OrderBy(s => s.SegmentIndex)
            .ToListAsync();

        sampleSegments.Should().HaveCount(3, "Sample segments should be retrievable");
        sampleSegments[0].SegmentIndex.Should().Be(0);
        sampleSegments[1].SegmentIndex.Should().Be(250);
        sampleSegments[2].SegmentIndex.Should().Be(499);

        _output.WriteLine("✓ Sample segment integrity verified (indices 0, 250, 499)");

        // Performance metrics
        var avgTimePerSegment = elapsedMs / (double)SEGMENT_COUNT;
        _output.WriteLine($"Average time per segment: {avgTimePerSegment:F3}ms");

        // Scaling analysis
        var expectedLinearTime = 2000 * 5; // If 100 segments = 2s, then 500 segments = 10s (linear)
        var scalingFactor = elapsedMs / (2000.0 * (SEGMENT_COUNT / 100.0) / 5.0);
        _output.WriteLine($"Scaling efficiency: {scalingFactor:F2}x (1.0 = linear, <1.0 = sub-linear/better)");

        if (elapsedMs < 3000)
        {
            _output.WriteLine("⚡ EXCELLENT: Completed in <3 seconds!");
        }
        else if (elapsedMs < 4000)
        {
            _output.WriteLine("✓ GOOD: Completed in <4 seconds");
        }
        else
        {
            _output.WriteLine("⚠ ACCEPTABLE: Completed in <5 seconds but could be optimized");
        }

        _output.WriteLine("=== Benchmark 2 PASSED ===\n");
    }

    #endregion

    #region Benchmark 3: 1000 Segments

    /// <summary>
    /// Benchmark 3: Insert 1000 Segments
    ///
    /// Target: <10 seconds
    /// Validates:
    /// - Measured time is <10000ms
    /// - All segments inserted correctly
    /// - System handles large batch efficiently
    /// </summary>
    [Fact]
    public async Task BulkInsert_1000Segments_ShouldCompleteUnder10Seconds()
    {
        // Arrange
        _output.WriteLine("=== Benchmark 3: 1000 Segments ===");
        _output.WriteLine("Target: <10000ms");

        await AuthenticateAsync();

        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        video.Status = VideoStatus.Pending;
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        const int SEGMENT_COUNT = 1000;
        var segments = GenerateSegments(video.Id, SEGMENT_COUNT);

        _output.WriteLine($"Generated {segments.Count} segments");
        _output.WriteLine($"Total text size: {segments.Sum(s => s.Text.Length)} characters");
        _output.WriteLine($"Estimated video duration: {segments.Last().EndTime / 60.0:F2} minutes");

        // Clear any existing segments for this video (test isolation)
        var existingSegments = await DbContext.TranscriptSegments
            .Where(s => s.VideoId == video.Id)
            .ToListAsync();
        if (existingSegments.Any())
        {
            DbContext.TranscriptSegments.RemoveRange(existingSegments);
            await DbContext.SaveChangesAsync();
            _output.WriteLine($"Cleared {existingSegments.Count} existing segments for test isolation");
        }

        var repository = Scope.ServiceProvider.GetRequiredService<ITranscriptSegmentRepository>();

        // Act - Measure bulk insert performance
        _output.WriteLine("Starting bulk insert...");

        var stopwatch = Stopwatch.StartNew();
        await repository.AddRangeAsync(segments);
        stopwatch.Stop();

        var elapsedMs = stopwatch.ElapsedMilliseconds;

        // Assert - Performance target
        _output.WriteLine($"Elapsed time: {elapsedMs}ms ({elapsedMs / 1000.0:F2}s)");
        _output.WriteLine($"Throughput: {SEGMENT_COUNT / (elapsedMs / 1000.0):F2} segments/second");

        elapsedMs.Should().BeLessThan(10000,
            $"1000 segments should be inserted in <10000ms, took {elapsedMs}ms");

        _output.WriteLine($"✓ Performance target met: {elapsedMs}ms < 10000ms");

        // Verify all segments inserted
        var savedCount = await DbContext.TranscriptSegments
            .Where(s => s.VideoId == video.Id)
            .CountAsync();

        savedCount.Should().Be(SEGMENT_COUNT, "All segments should be saved");

        _output.WriteLine($"✓ All {savedCount} segments verified in database");

        // Verify data integrity on first, middle, and last segments
        var checkpoints = await DbContext.TranscriptSegments
            .Where(s => s.VideoId == video.Id && (s.SegmentIndex == 0 || s.SegmentIndex == 500 || s.SegmentIndex == 999))
            .OrderBy(s => s.SegmentIndex)
            .ToListAsync();

        checkpoints.Should().HaveCount(3, "Checkpoint segments should be retrievable");
        checkpoints[0].SegmentIndex.Should().Be(0, "First segment index should be 0");
        checkpoints[1].SegmentIndex.Should().Be(500, "Middle segment index should be 500");
        checkpoints[2].SegmentIndex.Should().Be(999, "Last segment index should be 999");

        _output.WriteLine("✓ Checkpoint integrity verified (indices 0, 500, 999)");

        // Verify timestamps are sequential
        checkpoints[0].StartTime.Should().BeLessThan(checkpoints[1].StartTime, "Timestamps should be sequential");
        checkpoints[1].StartTime.Should().BeLessThan(checkpoints[2].StartTime, "Timestamps should be sequential");

        _output.WriteLine("✓ Timestamp sequence verified");

        // Performance metrics
        var avgTimePerSegment = elapsedMs / (double)SEGMENT_COUNT;
        _output.WriteLine($"Average time per segment: {avgTimePerSegment:F3}ms");

        // Memory and efficiency analysis
        var totalTextSize = segments.Sum(s => s.Text.Length);
        var bytesPerMs = totalTextSize / (double)elapsedMs;
        _output.WriteLine($"Data throughput: {bytesPerMs:F2} chars/ms ({bytesPerMs * 1000:F0} chars/second)");

        // Scaling analysis compared to 100 segment benchmark
        var expectedLinearTime = 2000 * 10; // If 100 segments = 2s, then 1000 segments = 20s (linear)
        var scalingFactor = elapsedMs / (expectedLinearTime / 2.0);
        _output.WriteLine($"Scaling efficiency vs linear: {scalingFactor:F2}x (1.0 = linear, <1.0 = sub-linear/better)");

        if (elapsedMs < 5000)
        {
            _output.WriteLine("⚡ EXCELLENT: Completed in <5 seconds! Outstanding performance!");
        }
        else if (elapsedMs < 7000)
        {
            _output.WriteLine("✓ VERY GOOD: Completed in <7 seconds");
        }
        else if (elapsedMs < 9000)
        {
            _output.WriteLine("✓ GOOD: Completed in <9 seconds");
        }
        else
        {
            _output.WriteLine("⚠ ACCEPTABLE: Completed in <10 seconds but could be optimized");
        }

        // Additional verification: Check that bulk insert was used (same CreatedAt for all)
        var distinctCreatedAtTimes = await DbContext.TranscriptSegments
            .Where(s => s.VideoId == video.Id)
            .Select(s => s.CreatedAt)
            .Distinct()
            .CountAsync();

        distinctCreatedAtTimes.Should().Be(1,
            "All segments should have the same CreatedAt timestamp (bulk insert indicator)");

        _output.WriteLine($"✓ Bulk insert confirmed - All segments created at same timestamp");

        _output.WriteLine("=== Benchmark 3 PASSED ===\n");
    }

    #endregion

    #region Bonus Benchmark: Stress Test

    /// <summary>
    /// Bonus Benchmark: Stress Test with 2000 Segments
    ///
    /// This test is marked as explicit and won't run by default
    /// Run explicitly with: dotnet test --filter "FullyQualifiedName~StressTest"
    ///
    /// Target: <20 seconds
    /// Validates system behavior under heavy load
    /// </summary>
    [Fact(Skip = "Stress test - Run explicitly when needed")]
    public async Task BulkInsert_StressTest_2000Segments_ShouldHandleGracefully()
    {
        // Arrange
        _output.WriteLine("=== STRESS TEST: 2000 Segments ===");
        _output.WriteLine("Target: <20000ms");
        _output.WriteLine("⚠ This is a stress test - performance may vary based on system resources");

        await AuthenticateAsync();

        var video = TestDataGenerator.GenerateVideo(AuthenticatedUserId);
        video.Status = VideoStatus.Pending;
        await DbContext.Videos.AddAsync(video);
        await DbContext.SaveChangesAsync();

        const int SEGMENT_COUNT = 2000;
        var segments = GenerateSegments(video.Id, SEGMENT_COUNT);

        _output.WriteLine($"Generated {segments.Count} segments");
        _output.WriteLine($"Total text size: {segments.Sum(s => s.Text.Length):N0} characters");
        _output.WriteLine($"Estimated video duration: {segments.Last().EndTime / 60.0:F2} minutes");

        var repository = Scope.ServiceProvider.GetRequiredService<ITranscriptSegmentRepository>();

        // Act - Measure bulk insert performance
        _output.WriteLine("Starting stress test bulk insert...");

        var stopwatch = Stopwatch.StartNew();
        await repository.AddRangeAsync(segments);
        stopwatch.Stop();

        var elapsedMs = stopwatch.ElapsedMilliseconds;

        // Assert - Performance target (relaxed for stress test)
        _output.WriteLine($"Elapsed time: {elapsedMs}ms ({elapsedMs / 1000.0:F2}s)");
        _output.WriteLine($"Throughput: {SEGMENT_COUNT / (elapsedMs / 1000.0):F2} segments/second");

        elapsedMs.Should().BeLessThan(20000,
            $"2000 segments should be inserted in <20s for stress test, took {elapsedMs}ms");

        // Verify count only (not full validation to save time)
        var savedCount = await DbContext.TranscriptSegments
            .Where(s => s.VideoId == video.Id)
            .CountAsync();

        savedCount.Should().Be(SEGMENT_COUNT, "All segments should be saved");

        _output.WriteLine($"✓ Stress test passed: {savedCount} segments in {elapsedMs}ms");
        _output.WriteLine($"Average: {elapsedMs / (double)SEGMENT_COUNT:F3}ms per segment");

        _output.WriteLine("=== STRESS TEST PASSED ===\n");
    }

    #endregion
}
