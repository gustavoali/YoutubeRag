/**
 * Video Ingestion Load Test
 *
 * Purpose: Test video ingestion endpoint under realistic load
 * Duration: 8 minutes total (2m ramp-up, 5m sustained, 1m ramp-down)
 * Load: 0 → 50 VUs
 *
 * This test validates:
 * - Video ingestion performance under sustained load
 * - p95 response time < 2s
 * - Error rate < 1%
 * - System stability during extended load
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Load configuration
const config = JSON.parse(open('../config/default.json'));
const BASE_URL = __ENV.BASE_URL || config.baseUrl;

// Load test data
const users = JSON.parse(open('../fixtures/users.json'));
const videos = JSON.parse(open('../fixtures/test-videos.json'));

// Custom metrics
const errorRate = new Rate('errors');
const videoIngestionDuration = new Trend('video_ingestion_duration');
const videoIngestionSuccess = new Rate('video_ingestion_success');
const authFailures = new Counter('auth_failures');

// Test configuration
export const options = {
  stages: [
    { duration: '2m', target: 50 },  // Ramp up to 50 VUs
    { duration: '5m', target: 50 },  // Stay at 50 VUs
    { duration: '1m', target: 0 },   // Ramp down to 0 VUs
  ],
  thresholds: {
    http_req_duration: ['p(95)<2000', 'p(99)<3000'],
    http_req_failed: ['rate<0.01'],
    video_ingestion_duration: ['p(95)<2000', 'p(99)<3000'],
    video_ingestion_success: ['rate>0.99'],
    errors: ['rate<0.01'],
  },
  tags: {
    test_type: 'load',
    scenario: 'video_ingestion',
    environment: __ENV.ENVIRONMENT || 'local',
  },
};

/**
 * Setup function - runs once before the test
 */
export function setup() {
  console.log('🎬 Starting Video Ingestion Load Test');
  console.log(`   Target: ${BASE_URL}`);
  console.log(`   Load Profile: 0 → 50 → 50 → 0 VUs`);
  console.log(`   Duration: 8 minutes`);

  return {
    baseUrl: BASE_URL,
    testUser: users.testUser,
    videos: videos.videos,
  };
}

/**
 * Main test function - runs for each VU iteration
 */
export default function (data) {
  const { baseUrl, testUser, videos } = data;

  // Authenticate
  const token = authenticate(baseUrl, testUser);
  if (!token) {
    authFailures.add(1);
    errorRate.add(1);
    sleep(5); // Back off on auth failure
    return;
  }

  // Select a random video
  const video = videos[Math.floor(Math.random() * videos.length)];

  // Test video ingestion
  testVideoIngestion(baseUrl, token, video);

  // Realistic think time between requests
  sleep(Math.random() * 3 + 2); // 2-5 seconds
}

/**
 * Authenticate and get JWT token
 */
function authenticate(baseUrl, user) {
  const loginPayload = JSON.stringify({
    email: user.email,
    password: user.password,
  });

  const response = http.post(
    `${baseUrl}/api/v1/auth/login`,
    loginPayload,
    {
      headers: { 'Content-Type': 'application/json' },
      tags: { name: 'auth_login' },
    }
  );

  const success = check(response, {
    'auth: status is 200': (r) => r.status === 200,
    'auth: has token': (r) => {
      try {
        return r.json('accessToken') !== undefined;
      } catch (e) {
        return false;
      }
    },
  });

  if (success) {
    return response.json('accessToken');
  }

  console.error(`Auth failed: ${response.status} - ${response.body}`);
  return null;
}

/**
 * Test video ingestion endpoint
 */
function testVideoIngestion(baseUrl, token, video) {
  const ingestionPayload = JSON.stringify({
    url: video.url,
    title: video.title,
    description: video.description,
    priority: 'normal',
  });

  const startTime = Date.now();

  const response = http.post(
    `${baseUrl}/api/v1/videos/ingest`,
    ingestionPayload,
    {
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      tags: { name: 'video_ingest' },
      timeout: '30s',
    }
  );

  const duration = Date.now() - startTime;
  videoIngestionDuration.add(duration);

  // Check response
  const success = check(response, {
    'video ingest: status is 200 or 409': (r) => r.status === 200 || r.status === 409,
    'video ingest: response time < 3s': (r) => r.timings.duration < 3000,
    'video ingest: has video id': (r) => {
      try {
        const body = r.json();
        return body.id !== undefined || body.videoId !== undefined;
      } catch (e) {
        return false;
      }
    },
  });

  if (success) {
    videoIngestionSuccess.add(1);
  } else {
    videoIngestionSuccess.add(0);
    errorRate.add(1);
    console.error(`Video ingestion failed: ${response.status} - ${response.body.substring(0, 200)}`);
  }

  // If successful, check progress endpoint
  if (response.status === 200) {
    try {
      const videoId = response.json('id') || response.json('videoId');
      if (videoId) {
        checkVideoProgress(baseUrl, token, videoId);
      }
    } catch (e) {
      // Ignore if we can't get video ID
    }
  }
}

/**
 * Check video processing progress
 */
function checkVideoProgress(baseUrl, token, videoId) {
  sleep(1); // Wait before checking progress

  const response = http.get(
    `${baseUrl}/api/v1/videos/${videoId}/progress`,
    {
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      tags: { name: 'video_progress' },
    }
  );

  check(response, {
    'progress: status is 200 or 404': (r) => r.status === 200 || r.status === 404,
    'progress: response time < 1s': (r) => r.timings.duration < 1000,
  });
}

/**
 * Teardown function - runs once at the end
 */
export function teardown(data) {
  console.log('✅ Video Ingestion Load Test Complete');
}

/**
 * Handle test summary
 */
export function handleSummary(data) {
  const summary = formatSummary(data);

  const result = {
    stdout: summary,
  };

  // Export results to JSON if specified
  if (__ENV.EXPORT_JSON) {
    result[__ENV.EXPORT_JSON] = JSON.stringify(data, null, 2);
  }

  // Export results to HTML if specified
  if (__ENV.EXPORT_HTML) {
    result[__ENV.EXPORT_HTML] = generateHtmlReport(data);
  }

  return result;
}

/**
 * Format summary for console output
 */
function formatSummary(data) {
  const httpReqs = data.metrics.http_reqs?.values.count || 0;
  const httpReqFailed = data.metrics.http_req_failed?.values.rate || 0;
  const httpReqDuration = data.metrics.http_req_duration?.values || {};
  const videoIngestionDur = data.metrics.video_ingestion_duration?.values || {};
  const videoIngestionSuccessRate = data.metrics.video_ingestion_success?.values.rate || 0;
  const authFailCount = data.metrics.auth_failures?.values.count || 0;

  const passed = httpReqFailed < 0.01 && videoIngestionSuccessRate > 0.99;

  return `
================================================================================
VIDEO INGESTION LOAD TEST SUMMARY
================================================================================
Total Requests:              ${httpReqs}
Failed Requests:             ${Math.round(httpReqs * httpReqFailed)}
Success Rate:                ${((1 - httpReqFailed) * 100).toFixed(2)}%

HTTP Request Duration:
  Average:                   ${(httpReqDuration.avg || 0).toFixed(2)}ms
  p95:                       ${(httpReqDuration['p(95)'] || 0).toFixed(2)}ms
  p99:                       ${(httpReqDuration['p(99)'] || 0).toFixed(2)}ms

Video Ingestion Metrics:
  Success Rate:              ${(videoIngestionSuccessRate * 100).toFixed(2)}%
  Average Duration:          ${(videoIngestionDur.avg || 0).toFixed(2)}ms
  p95 Duration:              ${(videoIngestionDur['p(95)'] || 0).toFixed(2)}ms
  p99 Duration:              ${(videoIngestionDur['p(99)'] || 0).toFixed(2)}ms

Authentication:
  Failed Logins:             ${authFailCount}

Throughput:                  ${(data.metrics.http_reqs?.values.rate || 0).toFixed(2)} req/s
Error Rate:                  ${((data.metrics.errors?.values.rate || 0) * 100).toFixed(2)}%

Data Transfer:
  Received:                  ${((data.metrics.data_received?.values.count || 0) / 1024 / 1024).toFixed(2)} MB
  Sent:                      ${((data.metrics.data_sent?.values.count || 0) / 1024).toFixed(2)} KB
================================================================================
Test Result: ${passed ? '✅ PASSED' : '❌ FAILED'}
================================================================================
${!passed ? `
Failure Reasons:
${httpReqFailed >= 0.01 ? `  - HTTP error rate (${(httpReqFailed * 100).toFixed(2)}%) exceeds threshold (1%)\n` : ''}
${videoIngestionSuccessRate <= 0.99 ? `  - Video ingestion success rate (${(videoIngestionSuccessRate * 100).toFixed(2)}%) below threshold (99%)\n` : ''}
================================================================================
` : ''}
`;
}

/**
 * Generate HTML report
 */
function generateHtmlReport(data) {
  const httpReqs = data.metrics.http_reqs?.values.count || 0;
  const httpReqFailed = data.metrics.http_req_failed?.values.rate || 0;
  const videoIngestionSuccessRate = data.metrics.video_ingestion_success?.values.rate || 0;
  const passed = httpReqFailed < 0.01 && videoIngestionSuccessRate > 0.99;

  return `<!DOCTYPE html>
<html>
<head>
  <title>Video Ingestion Load Test Report</title>
  <style>
    body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }
    .container { max-width: 1200px; margin: 0 auto; background: white; padding: 30px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
    h1 { color: #333; border-bottom: 3px solid #007bff; padding-bottom: 10px; }
    .status { font-size: 24px; font-weight: bold; margin: 20px 0; }
    .passed { color: #28a745; }
    .failed { color: #dc3545; }
    .metric-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 20px; margin: 20px 0; }
    .metric-card { background: #f8f9fa; padding: 15px; border-radius: 5px; border-left: 4px solid #007bff; }
    .metric-label { font-size: 12px; color: #666; text-transform: uppercase; }
    .metric-value { font-size: 24px; font-weight: bold; color: #333; margin-top: 5px; }
    .timestamp { color: #666; font-size: 14px; }
  </style>
</head>
<body>
  <div class="container">
    <h1>🎬 Video Ingestion Load Test Report</h1>
    <p class="timestamp">Generated: ${new Date().toISOString()}</p>
    <div class="status ${passed ? 'passed' : 'failed'}">
      ${passed ? '✅ PASSED' : '❌ FAILED'}
    </div>
    <div class="metric-grid">
      <div class="metric-card">
        <div class="metric-label">Total Requests</div>
        <div class="metric-value">${httpReqs}</div>
      </div>
      <div class="metric-card">
        <div class="metric-label">Success Rate</div>
        <div class="metric-value">${((1 - httpReqFailed) * 100).toFixed(2)}%</div>
      </div>
      <div class="metric-card">
        <div class="metric-label">Video Ingestion Success</div>
        <div class="metric-value">${(videoIngestionSuccessRate * 100).toFixed(2)}%</div>
      </div>
      <div class="metric-card">
        <div class="metric-label">p95 Response Time</div>
        <div class="metric-value">${(data.metrics.http_req_duration?.values['p(95)'] || 0).toFixed(0)}ms</div>
      </div>
    </div>
  </div>
</body>
</html>`;
}
