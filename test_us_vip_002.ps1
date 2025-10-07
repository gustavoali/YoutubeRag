# US-VIP-002 Validation Test Script
# Tests all acceptance criteria for API Error Handling

$baseUrl = "http://localhost:62788/api/v1"
$testVideo = "https://www.youtube.com/watch?v=jNQXAC9IVRw"
$authHeaders = @{
    'Authorization' = 'Bearer test-token'
    'Content-Type' = 'application/json'
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "US-VIP-002 VALIDATION TEST SUITE" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# AC1: Successful Ingestion (200 OK)
Write-Host "TEST AC1: Successful Video Ingestion" -ForegroundColor Yellow
Write-Host "Expected: 200 OK with VideoIngestionResponse" -ForegroundColor Gray
try {
    $body = @{url = $testVideo} | ConvertTo-Json
    $response = Invoke-WebRequest -Uri "$baseUrl/videos/ingest" -Method Post -Headers $authHeaders -Body $body -UseBasicParsing

    Write-Host "RESULT: PASS" -ForegroundColor Green
    Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Response:" -ForegroundColor Gray
    $response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10

    $global:ac1Pass = $true
} catch {
    Write-Host "RESULT: FAIL" -ForegroundColor Red
    Write-Host "Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    $global:ac1Pass = $false
}

Write-Host "`n----------------------------------------`n" -ForegroundColor Cyan

# AC2: Duplicate Video Detection (409 Conflict)
Write-Host "TEST AC2: Duplicate Video Detection" -ForegroundColor Yellow
Write-Host "Expected: 409 Conflict with ProblemDetails" -ForegroundColor Gray
try {
    $body = @{url = $testVideo} | ConvertTo-Json
    $response = Invoke-WebRequest -Uri "$baseUrl/videos/ingest" -Method Post -Headers $authHeaders -Body $body -UseBasicParsing

    Write-Host "RESULT: FAIL - Got 200 instead of 409" -ForegroundColor Red
    Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Red
    $global:ac2Pass = $false
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 409) {
        $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        $reader.Close()

        Write-Host "RESULT: PASS" -ForegroundColor Green
        Write-Host "Status Code: $statusCode" -ForegroundColor Green
        Write-Host "Response (ProblemDetails):" -ForegroundColor Gray
        $problemDetails = $responseBody | ConvertFrom-Json
        $problemDetails | ConvertTo-Json -Depth 10

        # Validate ProblemDetails format
        $hasAllFields = ($problemDetails.type -and $problemDetails.title -and $problemDetails.status -and
                        $problemDetails.detail -and $problemDetails.resourceId -and $problemDetails.resourceType -and
                        $problemDetails.traceId -and $problemDetails.timestamp)

        if ($hasAllFields) {
            Write-Host "`nProblemDetails Format: VALID (all required fields present)" -ForegroundColor Green
        } else {
            Write-Host "`nProblemDetails Format: INVALID (missing fields)" -ForegroundColor Red
        }

        $global:ac2Pass = $statusCode -eq 409 -and $hasAllFields
    } else {
        Write-Host "RESULT: FAIL - Got $statusCode instead of 409" -ForegroundColor Red
        $global:ac2Pass = $false
    }
}

Write-Host "`n----------------------------------------`n" -ForegroundColor Cyan

# AC3: Invalid URL (400 Bad Request)
Write-Host "TEST AC3: Invalid URL Handling" -ForegroundColor Yellow
Write-Host "Expected: 400 Bad Request with ProblemDetails" -ForegroundColor Gray
try {
    $body = @{url = 'not-a-valid-url'} | ConvertTo-Json
    $response = Invoke-WebRequest -Uri "$baseUrl/videos/ingest" -Method Post -Headers $authHeaders -Body $body -UseBasicParsing

    Write-Host "RESULT: FAIL - Got 200 instead of 400" -ForegroundColor Red
    Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Red
    $global:ac3Pass = $false
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 400) {
        $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        $reader.Close()

        Write-Host "RESULT: PASS" -ForegroundColor Green
        Write-Host "Status Code: $statusCode" -ForegroundColor Green
        Write-Host "Response (ProblemDetails):" -ForegroundColor Gray
        $responseBody | ConvertFrom-Json | ConvertTo-Json -Depth 10

        $global:ac3Pass = $true
    } else {
        Write-Host "RESULT: FAIL - Got $statusCode instead of 400" -ForegroundColor Red
        $global:ac3Pass = $false
    }
}

Write-Host "`n----------------------------------------`n" -ForegroundColor Cyan

# AC4: Missing Authentication (401 Unauthorized)
Write-Host "TEST AC4: Missing Authentication" -ForegroundColor Yellow
Write-Host "Expected: 401 Unauthorized" -ForegroundColor Gray
try {
    $noAuthHeaders = @{'Content-Type' = 'application/json'}
    $body = @{url = $testVideo} | ConvertTo-Json
    $response = Invoke-WebRequest -Uri "$baseUrl/videos/ingest" -Method Post -Headers $noAuthHeaders -Body $body -UseBasicParsing

    Write-Host "RESULT: FAIL - Got 200 instead of 401" -ForegroundColor Red
    Write-Host "Status Code: $($response.StatusCode)" -ForegroundColor Red
    $global:ac4Pass = $false
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 401) {
        Write-Host "RESULT: PASS" -ForegroundColor Green
        Write-Host "Status Code: $statusCode" -ForegroundColor Green
        $global:ac4Pass = $true
    } else {
        Write-Host "RESULT: FAIL - Got $statusCode instead of 401" -ForegroundColor Red
        $global:ac4Pass = $false
    }
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "VALIDATION SUMMARY" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "AC1 (Successful Ingestion):    $(if ($global:ac1Pass) {'PASS'} else {'FAIL'})" -ForegroundColor $(if ($global:ac1Pass) {'Green'} else {'Red'})
Write-Host "AC2 (Duplicate Detection):     $(if ($global:ac2Pass) {'PASS'} else {'FAIL'})" -ForegroundColor $(if ($global:ac2Pass) {'Green'} else {'Red'})
Write-Host "AC3 (Invalid URL):             $(if ($global:ac3Pass) {'PASS'} else {'FAIL'})" -ForegroundColor $(if ($global:ac3Pass) {'Green'} else {'Red'})
Write-Host "AC4 (Missing Auth):            $(if ($global:ac4Pass) {'PASS'} else {'FAIL'})" -ForegroundColor $(if ($global:ac4Pass) {'Green'} else {'Red'})

$allPass = $global:ac1Pass -and $global:ac2Pass -and $global:ac3Pass -and $global:ac4Pass
Write-Host "`nOVERALL: $(if ($allPass) {'PASS - US-VIP-002 VALIDATED'} else {'FAIL - Issues Found'})" -ForegroundColor $(if ($allPass) {'Green'} else {'Red'})
Write-Host "========================================`n" -ForegroundColor Cyan
