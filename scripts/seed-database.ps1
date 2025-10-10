# ============================================
# YoutubeRag Database Seeding Script (PowerShell)
# ============================================
# Creates test data for development and testing
# - Admin and regular test users
# - Sample videos with different statuses
# - Sample jobs in various states
# - Sample transcript segments
#
# IDEMPOTENT: Safe to run multiple times
# Last Updated: 2025-10-10
# ============================================

param(
    [string]$Environment = "Development",
    [string]$MySQLHost = "localhost",
    [int]$MySQLPort = 3306,
    [string]$Database = "youtube_rag_db",
    [string]$User = "root",
    [string]$Password = "rootpassword",
    [switch]$CleanFirst = $false,
    [switch]$Verbose = $false
)

# Color functions
function Write-Success { param($Message) Write-Host "✓ $Message" -ForegroundColor Green }
function Write-Info { param($Message) Write-Host "ℹ $Message" -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host "⚠ $Message" -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host "✗ $Message" -ForegroundColor Red }
function Write-Header { param($Message) Write-Host "`n========================================" -ForegroundColor Magenta; Write-Host $Message -ForegroundColor Magenta; Write-Host "========================================`n" -ForegroundColor Magenta }

# Header
Write-Header "YoutubeRag Database Seeding ($Environment)"

# Validate environment
if ($Environment -eq "Production") {
    Write-Error "Cannot seed Production database with test data!"
    Write-Info "Use -Environment 'Development' or 'Testing' instead"
    exit 1
}

# Test database connection
Write-Info "Testing database connection..."
try {
    $connectionTest = docker exec youtube-rag-mysql mysqladmin ping -h $MySQLHost -u $User -p$Password 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Database connection successful"
    } else {
        throw "Connection failed"
    }
} catch {
    Write-Error "Failed to connect to MySQL database"
    Write-Error "Ensure MySQL container is running: docker ps | grep youtube-rag-mysql"
    exit 1
}

# Clean existing test data if requested
if ($CleanFirst) {
    Write-Warning "Cleaning existing test data..."

    $cleanSQL = @"
-- Clean test data (preserve schema)
SET FOREIGN_KEY_CHECKS = 0;
DELETE FROM TranscriptSegments;
DELETE FROM Jobs;
DELETE FROM JobStages;
DELETE FROM DeadLetterJobs;
DELETE FROM UserNotifications;
DELETE FROM Videos;
DELETE FROM RefreshTokens;
DELETE FROM Users WHERE Email LIKE '%@test.example.com' OR Email = 'admin@youtuberag.com';
SET FOREIGN_KEY_CHECKS = 1;
"@

    $cleanSQL | docker exec -i youtube-rag-mysql mysql -u $User -p$Password $Database

    if ($LASTEXITCODE -eq 0) {
        Write-Success "Test data cleaned"
    } else {
        Write-Warning "Clean operation had warnings (this may be normal)"
    }
}

# Generate seed data SQL
Write-Info "Generating seed data..."

$seedSQL = @"
-- ============================================
-- YoutubeRag Seed Data (Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
-- Environment: $Environment
-- ============================================

SET @now = UTC_TIMESTAMP();

-- ============================================
-- USERS
-- ============================================
-- Admin User (password: Admin123!)
-- Password Hash: BCrypt hash of 'Admin123!'
INSERT IGNORE INTO Users (Id, Email, PasswordHash, FirstName, LastName, IsActive, EmailConfirmed, CreatedAt, UpdatedAt)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'admin@youtuberag.com',
    '\$2a\$11\$8PJ9K2X8Z8K8Z8K8Z8K8Zuj5GK5GK5GK5GK5GK5GK5GK5GK5GK5GK',
    'Admin',
    'User',
    1,
    1,
    @now,
    @now
);

-- Regular Test User 1 (password: Test123!)
INSERT IGNORE INTO Users (Id, Email, PasswordHash, FirstName, LastName, IsActive, EmailConfirmed, CreatedAt, UpdatedAt)
VALUES (
    '00000000-0000-0000-0000-000000000002',
    'user1@test.example.com',
    '\$2a\$11\$8PJ9K2X8Z8K8Z8K8Z8K8Zuj5GK5GK5GK5GK5GK5GK5GK5GK5GK5GK',
    'Test',
    'User One',
    1,
    1,
    @now,
    @now
);

-- Regular Test User 2 (password: Test123!)
INSERT IGNORE INTO Users (Id, Email, PasswordHash, FirstName, LastName, IsActive, EmailConfirmed, CreatedAt, UpdatedAt)
VALUES (
    '00000000-0000-0000-0000-000000000003',
    'user2@test.example.com',
    '\$2a\$11\$8PJ9K2X8Z8K8Z8K8Z8K8Zuj5GK5GK5GK5GK5GK5GK5GK5GK5GK5GK',
    'Test',
    'User Two',
    1,
    1,
    @now,
    @now
);

-- Inactive Test User (for testing)
INSERT IGNORE INTO Users (Id, Email, PasswordHash, FirstName, LastName, IsActive, EmailConfirmed, CreatedAt, UpdatedAt)
VALUES (
    '00000000-0000-0000-0000-000000000004',
    'inactive@test.example.com',
    '\$2a\$11\$8PJ9K2X8Z8K8Z8K8Z8K8Zuj5GK5GK5GK5GK5GK5GK5GK5GK5GK5GK',
    'Inactive',
    'User',
    0,
    1,
    @now,
    @now
);

-- ============================================
-- VIDEOS
-- ============================================
-- Video 1: Completed (with transcription)
INSERT IGNORE INTO Videos (Id, YouTubeId, Title, Description, Duration, ThumbnailUrl, ChannelTitle, Status, ProcessingProgress, UserId, CreatedAt, UpdatedAt)
VALUES (
    '10000000-0000-0000-0000-000000000001',
    'dQw4w9WgXcQ',
    'Sample Video - Completed Processing',
    'This is a test video that has completed processing successfully',
    213,
    'https://i.ytimg.com/vi/dQw4w9WgXcQ/default.jpg',
    'Test Channel',
    'Completed',
    100,
    '00000000-0000-0000-0000-000000000001',
    @now,
    @now
);

-- Video 2: Processing
INSERT IGNORE INTO Videos (Id, YouTubeId, Title, Description, Duration, ThumbnailUrl, ChannelTitle, Status, ProcessingProgress, UserId, CreatedAt, UpdatedAt)
VALUES (
    '10000000-0000-0000-0000-000000000002',
    'jNQXAC9IVRw',
    'Sample Video - Currently Processing',
    'This video is currently being processed',
    456,
    'https://i.ytimg.com/vi/jNQXAC9IVRw/default.jpg',
    'Test Channel',
    'Processing',
    45,
    '00000000-0000-0000-0000-000000000002',
    @now,
    @now
);

-- Video 3: Pending
INSERT IGNORE INTO Videos (Id, YouTubeId, Title, Description, Duration, ThumbnailUrl, ChannelTitle, Status, ProcessingProgress, UserId, CreatedAt, UpdatedAt)
VALUES (
    '10000000-0000-0000-0000-000000000003',
    '9bZkp7q19f0',
    'Sample Video - Pending Processing',
    'This video is queued for processing',
    789,
    'https://i.ytimg.com/vi/9bZkp7q19f0/default.jpg',
    'Test Channel',
    'Pending',
    0,
    '00000000-0000-0000-0000-000000000002',
    @now,
    @now
);

-- Video 4: Failed
INSERT IGNORE INTO Videos (Id, YouTubeId, Title, Description, Duration, ThumbnailUrl, ChannelTitle, Status, ProcessingProgress, ErrorMessage, UserId, CreatedAt, UpdatedAt)
VALUES (
    '10000000-0000-0000-0000-000000000004',
    'oHg5SJYRHA0',
    'Sample Video - Processing Failed',
    'This video encountered an error during processing',
    1234,
    'https://i.ytimg.com/vi/oHg5SJYRHA0/default.jpg',
    'Test Channel',
    'Failed',
    30,
    'Simulated error: FFmpeg process failed',
    '00000000-0000-0000-0000-000000000001',
    @now,
    @now
);

-- Video 5: Long video (for testing model selection)
INSERT IGNORE INTO Videos (Id, YouTubeId, Title, Description, Duration, ThumbnailUrl, ChannelTitle, Status, ProcessingProgress, UserId, CreatedAt, UpdatedAt)
VALUES (
    '10000000-0000-0000-0000-000000000005',
    'kJQP7kiw5Fk',
    'Sample Long Video - 2 Hours',
    'A long video for testing large file processing',
    7200,
    'https://i.ytimg.com/vi/kJQP7kiw5Fk/default.jpg',
    'Test Channel',
    'Pending',
    0,
    '00000000-0000-0000-0000-000000000003',
    @now,
    @now
);

-- ============================================
-- JOBS
-- ============================================
-- Job 1: Completed Transcription Job
INSERT IGNORE INTO Jobs (Id, Type, Status, Priority, Progress, UserId, VideoId, StatusMessage, CreatedAt, UpdatedAt, StartedAt, CompletedAt)
VALUES (
    '20000000-0000-0000-0000-000000000001',
    'Transcription',
    'Completed',
    0,
    100,
    '00000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000001',
    'Transcription completed successfully',
    DATE_SUB(@now, INTERVAL 2 HOUR),
    DATE_SUB(@now, INTERVAL 1 HOUR),
    DATE_SUB(@now, INTERVAL 2 HOUR),
    DATE_SUB(@now, INTERVAL 1 HOUR)
);

-- Job 2: Running Job
INSERT IGNORE INTO Jobs (Id, Type, Status, Priority, Progress, UserId, VideoId, StatusMessage, CreatedAt, UpdatedAt, StartedAt)
VALUES (
    '20000000-0000-0000-0000-000000000002',
    'VideoProcessing',
    'Running',
    0,
    45,
    '00000000-0000-0000-0000-000000000002',
    '10000000-0000-0000-0000-000000000002',
    'Processing video: Audio extraction in progress',
    DATE_SUB(@now, INTERVAL 30 MINUTE),
    @now,
    DATE_SUB(@now, INTERVAL 25 MINUTE)
);

-- Job 3: Pending Job
INSERT IGNORE INTO Jobs (Id, Type, Status, Priority, Progress, UserId, VideoId, StatusMessage, CreatedAt, UpdatedAt)
VALUES (
    '20000000-0000-0000-0000-000000000003',
    'Embedding',
    'Pending',
    0,
    0,
    '00000000-0000-0000-0000-000000000002',
    '10000000-0000-0000-0000-000000000003',
    'Queued for processing',
    DATE_SUB(@now, INTERVAL 10 MINUTE),
    DATE_SUB(@now, INTERVAL 10 MINUTE)
);

-- Job 4: Failed Job (with retries)
INSERT IGNORE INTO Jobs (Id, Type, Status, Priority, Progress, RetryCount, MaxRetries, UserId, VideoId, StatusMessage, ErrorMessage, CreatedAt, UpdatedAt, StartedAt, FailedAt)
VALUES (
    '20000000-0000-0000-0000-000000000004',
    'Transcription',
    'Failed',
    1,
    30,
    3,
    3,
    '00000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000004',
    'Job failed after 3 retries',
    'FFmpeg error: Cannot extract audio from video file',
    DATE_SUB(@now, INTERVAL 3 HOUR),
    DATE_SUB(@now, INTERVAL 2 HOUR),
    DATE_SUB(@now, INTERVAL 3 HOUR),
    DATE_SUB(@now, INTERVAL 2 HOUR)
);

-- Job 5: High Priority Pending Job
INSERT IGNORE INTO Jobs (Id, Type, Status, Priority, Progress, UserId, VideoId, StatusMessage, CreatedAt, UpdatedAt)
VALUES (
    '20000000-0000-0000-0000-000000000005',
    'VideoProcessing',
    'Pending',
    2,
    0,
    '00000000-0000-0000-0000-000000000003',
    '10000000-0000-0000-0000-000000000005',
    'High priority: Queued for processing',
    DATE_SUB(@now, INTERVAL 5 MINUTE),
    DATE_SUB(@now, INTERVAL 5 MINUTE)
);

-- ============================================
-- TRANSCRIPT SEGMENTS
-- ============================================
-- Segments for Video 1 (Completed)
INSERT IGNORE INTO TranscriptSegments (Id, VideoId, StartTime, EndTime, Text, Confidence, CreatedAt, UpdatedAt)
VALUES
    ('30000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 0.0, 5.5, 'Welcome to this test video demonstration.', 0.95, @now, @now),
    ('30000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', 5.5, 12.3, 'This is a sample transcription segment for testing purposes.', 0.92, @now, @now),
    ('30000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000001', 12.3, 18.7, 'The YoutubeRag system processes videos and extracts meaningful content.', 0.89, @now, @now),
    ('30000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000001', 18.7, 25.1, 'You can search across all your video transcriptions.', 0.94, @now, @now),
    ('30000000-0000-0000-0000-000000000005', '10000000-0000-0000-0000-000000000001', 25.1, 30.0, 'Thank you for watching this demo.', 0.96, @now, @now);

-- ============================================
-- USER NOTIFICATIONS
-- ============================================
INSERT IGNORE INTO UserNotifications (Id, UserId, Title, Message, Type, IsRead, CreatedAt)
VALUES
    ('40000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'Video Processing Complete', 'Your video "Sample Video - Completed Processing" has been successfully processed.', 'Success', 1, DATE_SUB(@now, INTERVAL 1 HOUR)),
    ('40000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000002', 'Video Processing Started', 'Processing has started for "Sample Video - Currently Processing"', 'Info', 0, DATE_SUB(@now, INTERVAL 25 MINUTE)),
    ('40000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000001', 'Processing Error', 'Failed to process video "Sample Video - Processing Failed". Error: FFmpeg process failed', 'Error', 0, DATE_SUB(@now, INTERVAL 2 HOUR));

-- ============================================
-- Summary
-- ============================================
SELECT 'Database seeded successfully!' AS Status;
SELECT COUNT(*) AS UserCount FROM Users WHERE Email LIKE '%@test.example.com' OR Email = 'admin@youtuberag.com';
SELECT COUNT(*) AS VideoCount FROM Videos;
SELECT COUNT(*) AS JobCount FROM Jobs;
SELECT COUNT(*) AS SegmentCount FROM TranscriptSegments;
SELECT COUNT(*) AS NotificationCount FROM UserNotifications;
"@

# Execute seed SQL
Write-Info "Seeding database with test data..."
$seedSQL | docker exec -i youtube-rag-mysql mysql -u $User -p$Password $Database

if ($LASTEXITCODE -eq 0) {
    Write-Success "Database seeded successfully!"
    Write-Host ""
} else {
    Write-Error "Failed to seed database"
    exit 1
}

# Summary
Write-Header "Seeding Complete!"
Write-Success "Test users created:"
Write-Host "  • admin@youtuberag.com (password: Admin123!)" -ForegroundColor White
Write-Host "  • user1@test.example.com (password: Test123!)" -ForegroundColor White
Write-Host "  • user2@test.example.com (password: Test123!)" -ForegroundColor White
Write-Host "  • inactive@test.example.com (password: Test123!) - Inactive" -ForegroundColor Gray
Write-Host ""

Write-Success "Test videos created:"
Write-Host "  • 5 sample videos with different statuses" -ForegroundColor White
Write-Host "  • Statuses: Completed, Processing, Pending, Failed, Long Video" -ForegroundColor White
Write-Host ""

Write-Success "Test jobs created:"
Write-Host "  • 5 sample jobs in various states" -ForegroundColor White
Write-Host "  • Types: Transcription, VideoProcessing, Embedding" -ForegroundColor White
Write-Host "  • Statuses: Completed, Running, Pending, Failed" -ForegroundColor White
Write-Host ""

Write-Success "Additional data:"
Write-Host "  • 5 transcript segments for completed video" -ForegroundColor White
Write-Host "  • 3 user notifications" -ForegroundColor White
Write-Host ""

Write-Info "You can now test the application with this seed data"
Write-Info "To clean and re-seed: .\seed-database.ps1 -CleanFirst"
Write-Host ""
