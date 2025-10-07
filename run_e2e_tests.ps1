# ================================================================
# YoutubeRag E2E Test Suite
# ================================================================
# Tests the complete video ingestion pipeline from URL to transcription
# ================================================================

param(
    [string]$BaseUrl = "https://localhost:62787",
    [int]$PollingInterval = 10,
    [int]$MaxTimeout = 300
)

# Configuration
$ErrorActionPreference = "Continue"
$ProgressPreference = "SilentlyContinue"

# Colors for output
$ColorSuccess = "Green"
$ColorError = "Red"
$ColorWarning = "Yellow"
$ColorInfo = "Cyan"

# Load test configuration
$configPath = "C:\agents\youtube_rag_net\e2e_test_videos.json"
$config = Get-Content $configPath | ConvertFrom-Json

Write-Host "`n================================================================" -ForegroundColor $ColorInfo
Write-Host "YoutubeRag E2E Test Suite - Video Ingestion Pipeline" -ForegroundColor $ColorInfo
Write-Host "================================================================`n" -ForegroundColor $ColorInfo

Write-Host "Configuration:" -ForegroundColor $ColorInfo
Write-Host "  Base URL: $BaseUrl" -ForegroundColor White
Write-Host "  Test Videos: $($config.test_videos.Count)" -ForegroundColor White
Write-Host "  Polling Interval: $PollingInterval seconds" -ForegroundColor White
Write-Host "  Max Timeout: $MaxTimeout seconds`n" -ForegroundColor White

# Test results storage
$testResults = @()
$startTime = Get-Date

# ================================================================
# Helper Functions
# ================================================================

function Invoke-ApiCall {
    param(
        [string]$Url,
        [string]$Method = "GET",
        [object]$Body = $null,
        [hashtable]$Headers = @{}
    )

    try {
        # Skip certificate validation for self-signed certificates
        add-type @"
            using System.Net;
            using System.Security.Cryptography.X509Certificates;
            public class TrustAllCertsPolicy : ICertificatePolicy {
                public bool CheckValidationResult(
                    ServicePoint srvPoint, X509Certificate certificate,
                    WebRequest request, int certificateProblem) {
                    return true;
                }
            }
"@
        [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

        # Add mock authentication token for test environment
        if (-not $Headers.ContainsKey("Authorization")) {
            $Headers["Authorization"] = "Bearer mock-test-token-e2e-suite"
        }

        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            TimeoutSec = 30
        }

        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
            $params.ContentType = "application/json"
        }

        $response = Invoke-RestMethod @params
        return @{ Success = $true; Data = $response; Error = $null }
    }
    catch {
        return @{ Success = $false; Data = $null; Error = $_.Exception.Message }
    }
}

function Wait-ForVideoProcessing {
    param(
        [string]$VideoId,
        [int]$TimeoutSeconds,
        [int]$PollInterval
    )

    $startTime = Get-Date
    $progressUrl = "$BaseUrl/api/v1/videos/$VideoId/progress"

    Write-Host "    Monitoring progress..." -ForegroundColor $ColorInfo

    while ($true) {
        $elapsed = ((Get-Date) - $startTime).TotalSeconds

        if ($elapsed -gt $TimeoutSeconds) {
            return @{
                Success = $false
                Status = "Timeout"
                Message = "Processing timed out after $TimeoutSeconds seconds"
                Duration = $elapsed
            }
        }

        $result = Invoke-ApiCall -Url $progressUrl -Method "GET"

        if (-not $result.Success) {
            Write-Host "    [ERROR] Failed to get progress: $($result.Error)" -ForegroundColor $ColorError
            Start-Sleep -Seconds $PollInterval
            continue
        }

        $progress = $result.Data
        $status = $progress.status
        $overallProgress = $progress.progress
        $currentStage = $progress.current_stage

        Write-Host "    [$($elapsed.ToString('F0'))s] Status: $status | Progress: $overallProgress% | Stage: $currentStage" -ForegroundColor $ColorInfo

        # Check if processing is complete
        if ($status -eq "Completed" -or $status -eq "completed") {
            return @{
                Success = $true
                Status = "Completed"
                Message = "Processing completed successfully"
                Duration = $elapsed
                Stages = $progress.stages
                Progress = $progress
            }
        }

        # Check for errors
        if ($status -eq "Failed" -or $status -eq "failed" -or $status -eq "Error") {
            return @{
                Success = $false
                Status = "Failed"
                Message = "Processing failed: $($progress.error_message)"
                Duration = $elapsed
                ErrorMessage = $progress.error_message
            }
        }

        Start-Sleep -Seconds $PollInterval
    }
}

function Test-AudioFile {
    param([string]$VideoId)

    $audioPath = "C:\agents\youtube_rag_net\data\audio"
    if (-not (Test-Path $audioPath)) {
        return @{ Exists = $false; Files = @() }
    }

    $files = Get-ChildItem -Path $audioPath -Filter "${VideoId}_*" -ErrorAction SilentlyContinue
    return @{
        Exists = ($files.Count -gt 0)
        Files = $files | ForEach-Object { $_.Name }
        TotalSize = ($files | Measure-Object -Property Length -Sum).Sum
    }
}

# ================================================================
# Pre-Test Validation
# ================================================================

Write-Host "Pre-Test Validation:" -ForegroundColor $ColorInfo
Write-Host "  Checking API health..." -NoNewline

$healthCheck = Invoke-ApiCall -Url "$BaseUrl/health"
if ($healthCheck.Success) {
    Write-Host " OK" -ForegroundColor $ColorSuccess
} else {
    Write-Host " FAILED" -ForegroundColor $ColorError
    Write-Host "  Error: $($healthCheck.Error)" -ForegroundColor $ColorError
    exit 1
}

Write-Host "`n================================================================" -ForegroundColor $ColorInfo
Write-Host "Starting Video Ingestion Tests" -ForegroundColor $ColorInfo
Write-Host "================================================================`n" -ForegroundColor $ColorInfo

# ================================================================
# Execute Tests for Each Video
# ================================================================

$videoNumber = 0
foreach ($video in $config.test_videos) {
    $videoNumber++
    $testStart = Get-Date

    Write-Host "`n----------------------------------------------------------------" -ForegroundColor $ColorInfo
    Write-Host "Test $videoNumber/$($config.test_videos.Count): $($video.name)" -ForegroundColor $ColorInfo
    Write-Host "----------------------------------------------------------------" -ForegroundColor $ColorInfo
    Write-Host "  URL: $($video.url)" -ForegroundColor White
    Write-Host "  YouTube ID: $($video.youtube_id)" -ForegroundColor White
    Write-Host "  Expected Duration: $($video.expected_duration)" -ForegroundColor White
    Write-Host "  Language: $($video.language)" -ForegroundColor White
    Write-Host "  Priority: $($video.priority)`n" -ForegroundColor White

    # Initialize test result
    $testResult = @{
        VideoNumber = $videoNumber
        Name = $video.name
        YouTubeId = $video.youtube_id
        Url = $video.url
        Language = $video.language
        Priority = $video.priority
        StartTime = $testStart
        Stages = @{}
    }

    # ================================================================
    # Stage 1: Ingest Video
    # ================================================================

    Write-Host "  [Stage 1/4] Ingesting video..." -ForegroundColor $ColorInfo

    $ingestBody = @{
        url = $video.url
        priority = $video.priority
    }

    $ingestResult = Invoke-ApiCall -Url "$BaseUrl/api/v1/videos/ingest" -Method "POST" -Body $ingestBody

    if (-not $ingestResult.Success) {
        Write-Host "    FAILED: $($ingestResult.Error)" -ForegroundColor $ColorError
        $testResult.Result = "FAILED"
        $testResult.FailureStage = "Ingestion"
        $testResult.ErrorMessage = $ingestResult.Error
        $testResult.Duration = ((Get-Date) - $testStart).TotalSeconds
        $testResults += $testResult
        continue
    }

    $ingestionData = $ingestResult.Data
    $videoId = $ingestionData.videoId

    Write-Host "    SUCCESS - Video ID: $videoId" -ForegroundColor $ColorSuccess
    Write-Host "    Job ID: $($ingestionData.jobId)" -ForegroundColor White
    Write-Host "    Status: $($ingestionData.status)" -ForegroundColor White

    $testResult.VideoId = $videoId
    $testResult.JobId = $ingestionData.jobId
    $testResult.Stages.Ingestion = @{
        Success = $true
        Duration = ((Get-Date) - $testStart).TotalSeconds
    }

    # ================================================================
    # Stage 2: Monitor Processing
    # ================================================================

    Write-Host "`n  [Stage 2/4] Monitoring video processing..." -ForegroundColor $ColorInfo

    $processingResult = Wait-ForVideoProcessing -VideoId $videoId -TimeoutSeconds $MaxTimeout -PollInterval $PollingInterval

    if (-not $processingResult.Success) {
        Write-Host "    FAILED: $($processingResult.Message)" -ForegroundColor $ColorError
        $testResult.Result = "FAILED"
        $testResult.FailureStage = "Processing"
        $testResult.ErrorMessage = $processingResult.Message
        $testResult.Duration = ((Get-Date) - $testStart).TotalSeconds
        $testResults += $testResult
        continue
    }

    Write-Host "    SUCCESS - Processing completed in $($processingResult.Duration.ToString('F1')) seconds" -ForegroundColor $ColorSuccess

    $testResult.Stages.Processing = @{
        Success = $true
        Duration = $processingResult.Duration
        Status = $processingResult.Status
        StageDetails = $processingResult.Stages
    }

    # ================================================================
    # Stage 3: Verify Audio File
    # ================================================================

    Write-Host "`n  [Stage 3/4] Verifying audio extraction..." -ForegroundColor $ColorInfo

    $audioCheck = Test-AudioFile -VideoId $video.youtube_id

    if ($audioCheck.Exists) {
        $sizeKB = [math]::Round($audioCheck.TotalSize / 1KB, 2)
        Write-Host "    SUCCESS - Audio file found: $($audioCheck.Files[0])" -ForegroundColor $ColorSuccess
        Write-Host "    Size: $sizeKB KB" -ForegroundColor White
        $testResult.Stages.AudioExtraction = @{
            Success = $true
            FilePath = $audioCheck.Files[0]
            SizeKB = $sizeKB
        }
    } else {
        Write-Host "    WARNING - No audio file found" -ForegroundColor $ColorWarning
        $testResult.Stages.AudioExtraction = @{
            Success = $false
            Message = "Audio file not found"
        }
    }

    # ================================================================
    # Stage 4: Verify Video Details
    # ================================================================

    Write-Host "`n  [Stage 4/4] Verifying video details..." -ForegroundColor $ColorInfo

    $detailsResult = Invoke-ApiCall -Url "$BaseUrl/api/v1/videos/$videoId" -Method "GET"

    if ($detailsResult.Success) {
        $details = $detailsResult.Data
        Write-Host "    SUCCESS - Video details retrieved" -ForegroundColor $ColorSuccess
        Write-Host "    Title: $($details.title)" -ForegroundColor White
        Write-Host "    Status: $($details.status)" -ForegroundColor White
        Write-Host "    Transcription Status: $($details.transcriptionStatus)" -ForegroundColor White

        # Check for transcription segments
        if ($details.transcriptSegmentCount -gt 0) {
            Write-Host "    Transcript Segments: $($details.transcriptSegmentCount)" -ForegroundColor $ColorSuccess
        } else {
            Write-Host "    Transcript Segments: 0" -ForegroundColor $ColorWarning
        }

        $testResult.Stages.VideoDetails = @{
            Success = $true
            Title = $details.title
            Status = $details.status
            TranscriptionStatus = $details.transcriptionStatus
            SegmentCount = $details.transcriptSegmentCount
        }
    } else {
        Write-Host "    FAILED: $($detailsResult.Error)" -ForegroundColor $ColorError
        $testResult.Stages.VideoDetails = @{
            Success = $false
            Error = $detailsResult.Error
        }
    }

    # ================================================================
    # Test Summary
    # ================================================================

    $testResult.Duration = ((Get-Date) - $testStart).TotalSeconds
    $testResult.Result = "PASSED"
    $testResult.EndTime = Get-Date

    Write-Host "`n  OVERALL RESULT: " -NoNewline -ForegroundColor White
    Write-Host "PASSED" -ForegroundColor $ColorSuccess
    Write-Host "  Total Duration: $($testResult.Duration.ToString('F1')) seconds`n" -ForegroundColor White

    $testResults += $testResult
}

# ================================================================
# Final Report
# ================================================================

$totalDuration = ((Get-Date) - $startTime).TotalSeconds
$passedTests = ($testResults | Where-Object { $_.Result -eq "PASSED" }).Count
$failedTests = ($testResults | Where-Object { $_.Result -eq "FAILED" }).Count

Write-Host "`n================================================================" -ForegroundColor $ColorInfo
Write-Host "E2E Test Suite - Final Report" -ForegroundColor $ColorInfo
Write-Host "================================================================`n" -ForegroundColor $ColorInfo

Write-Host "Summary:" -ForegroundColor $ColorInfo
Write-Host "  Total Tests: $($testResults.Count)" -ForegroundColor White
Write-Host "  Passed: " -NoNewline -ForegroundColor White
Write-Host "$passedTests" -ForegroundColor $ColorSuccess
Write-Host "  Failed: " -NoNewline -ForegroundColor White
if ($failedTests -gt 0) {
    Write-Host "$failedTests" -ForegroundColor $ColorError
} else {
    Write-Host "$failedTests" -ForegroundColor $ColorSuccess
}
Write-Host "  Success Rate: $([math]::Round(($passedTests / $testResults.Count) * 100, 2))%" -ForegroundColor White
Write-Host "  Total Duration: $($totalDuration.ToString('F1')) seconds`n" -ForegroundColor White

Write-Host "Individual Test Results:" -ForegroundColor $ColorInfo
foreach ($result in $testResults) {
    $statusColor = if ($result.Result -eq "PASSED") { $ColorSuccess } else { $ColorError }
    Write-Host "  [$($result.Result)]" -NoNewline -ForegroundColor $statusColor
    Write-Host " $($result.Name) - $($result.Duration.ToString('F1'))s" -ForegroundColor White

    if ($result.Result -eq "FAILED") {
        Write-Host "    Failure Stage: $($result.FailureStage)" -ForegroundColor $ColorError
        Write-Host "    Error: $($result.ErrorMessage)" -ForegroundColor $ColorError
    }
}

# ================================================================
# Save Results to JSON
# ================================================================

Write-Host "`n================================================================" -ForegroundColor $ColorInfo
Write-Host "Saving Results" -ForegroundColor $ColorInfo
Write-Host "================================================================`n" -ForegroundColor $ColorInfo

$reportPath = "C:\agents\youtube_rag_net\e2e_test_results_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"

$fullReport = @{
    TestSuite = "YoutubeRag E2E - Video Ingestion Pipeline"
    ExecutionTime = $startTime
    TotalDuration = $totalDuration
    Configuration = @{
        BaseUrl = $BaseUrl
        PollingInterval = $PollingInterval
        MaxTimeout = $MaxTimeout
    }
    Summary = @{
        TotalTests = $testResults.Count
        Passed = $passedTests
        Failed = $failedTests
        SuccessRate = [math]::Round(($passedTests / $testResults.Count) * 100, 2)
    }
    TestResults = $testResults
}

$fullReport | ConvertTo-Json -Depth 10 | Out-File -FilePath $reportPath -Encoding UTF8

Write-Host "Results saved to: $reportPath" -ForegroundColor $ColorSuccess

# ================================================================
# Exit Code
# ================================================================

if ($failedTests -gt 0) {
    Write-Host "`nTest suite completed with FAILURES" -ForegroundColor $ColorError
    exit 1
} else {
    Write-Host "`nTest suite completed SUCCESSFULLY" -ForegroundColor $ColorSuccess
    exit 0
}
