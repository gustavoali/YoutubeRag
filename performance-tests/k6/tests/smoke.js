/**
 * Smoke Test - API Health & Basic Load
 *
 * Purpose: Quick validation that all critical endpoints are functional
 * Duration: 30 seconds
 * Load: 10 virtual users
 *
 * This test verifies:
 * - All critical endpoints return 2xx status codes
 * - Response times are within acceptable limits
 * - No critical errors occur under light load
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Load configuration
const config = JSON.parse(open('../config/default.json'));
const BASE_URL = __ENV.BASE_URL || config.baseUrl;

// Load test data
const users = JSON.parse(open('../fixtures/users.json'));
const searchQueries = JSON.parse(open('../fixtures/search-queries.json'));

// Custom metrics
const errorRate = new Rate('errors');

// Test configuration
export const options = {
  vus: 10,
  duration: '30s',
  thresholds: {
    http_req_duration: ['p(95)<3000', 'p(99)<5000'],
    http_req_failed: ['rate<0.05'],
    errors: ['rate<0.1'],
  },
  tags: {
    test_type: 'smoke',
    environment: __ENV.ENVIRONMENT || 'local',
  },
};

/**
 * Setup function - runs once per VU at the start
 */
export function setup() {
  console.log('🔥 Starting Smoke Test');
  console.log(`   Target: ${BASE_URL}`);
  console.log(`   VUs: ${options.vus}`);
  console.log(`   Duration: ${options.duration}`);

  return {
    baseUrl: BASE_URL,
    testUser: users.testUser,
  };
}

/**
 * Main test function - runs continuously for each VU
 */
export default function (data) {
  const { baseUrl, testUser } = data;

  // Test 1: Health Check Endpoint
  testHealthEndpoints(baseUrl);

  sleep(1);

  // Test 2: Authentication Flow
  const token = testAuthentication(baseUrl, testUser);

  if (!token) {
    errorRate.add(1);
    return;
  }

  sleep(1);

  // Test 3: Video Listing
  testVideoListing(baseUrl, token);

  sleep(1);

  // Test 4: Search Functionality
  testSearch(baseUrl, token);

  sleep(2);
}

/**
 * Test health check endpoints
 */
function testHealthEndpoints(baseUrl) {
  const healthEndpoints = [
    { name: 'health', path: '/health' },
    { name: 'ready', path: '/ready' },
    { name: 'live', path: '/live' },
  ];

  healthEndpoints.forEach((endpoint) => {
    const response = http.get(`${baseUrl}${endpoint.path}`, {
      tags: { name: `health_${endpoint.name}` },
    });

    const success = check(response, {
      [`${endpoint.name}: status is 200`]: (r) => r.status === 200,
      [`${endpoint.name}: response time < 1s`]: (r) => r.timings.duration < 1000,
    });

    if (!success) {
      errorRate.add(1);
      console.error(`Health check failed for ${endpoint.name}: ${response.status}`);
    }
  });
}

/**
 * Test authentication endpoints
 */
function testAuthentication(baseUrl, user) {
  const loginPayload = JSON.stringify({
    email: user.email,
    password: user.password,
  });

  const loginResponse = http.post(
    `${baseUrl}/api/v1/auth/login`,
    loginPayload,
    {
      headers: { 'Content-Type': 'application/json' },
      tags: { name: 'auth_login' },
    }
  );

  const loginSuccess = check(loginResponse, {
    'auth login: status is 200': (r) => r.status === 200,
    'auth login: has access token': (r) => {
      try {
        return r.json('accessToken') !== undefined;
      } catch (e) {
        return false;
      }
    },
  });

  if (!loginSuccess) {
    errorRate.add(1);
    console.error(`Login failed: ${loginResponse.status} - ${loginResponse.body}`);
    return null;
  }

  const token = loginResponse.json('accessToken');

  // Test /me endpoint
  const meResponse = http.get(`${baseUrl}/api/v1/auth/me`, {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    tags: { name: 'auth_me' },
  });

  const meSuccess = check(meResponse, {
    'auth me: status is 200': (r) => r.status === 200,
    'auth me: has user data': (r) => {
      try {
        return r.json('email') !== undefined;
      } catch (e) {
        return false;
      }
    },
  });

  if (!meSuccess) {
    errorRate.add(1);
  }

  return token;
}

/**
 * Test video listing endpoint
 */
function testVideoListing(baseUrl, token) {
  const response = http.get(`${baseUrl}/api/v1/videos?page=1&pageSize=20`, {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    tags: { name: 'videos_list' },
  });

  const success = check(response, {
    'videos list: status is 200': (r) => r.status === 200,
    'videos list: response time < 2s': (r) => r.timings.duration < 2000,
    'videos list: has videos array': (r) => {
      try {
        return r.json('videos') !== undefined;
      } catch (e) {
        return false;
      }
    },
  });

  if (!success) {
    errorRate.add(1);
  }
}

/**
 * Test search functionality
 */
function testSearch(baseUrl, token) {
  const query = searchQueries.queries[Math.floor(Math.random() * searchQueries.queries.length)];

  const searchPayload = JSON.stringify({
    query: query.query,
    maxResults: 10,
    minRelevanceScore: 0.5,
  });

  const response = http.post(
    `${baseUrl}/api/v1/search/semantic`,
    searchPayload,
    {
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      tags: { name: 'search_semantic' },
    }
  );

  const success = check(response, {
    'search: status is 200 or 400': (r) => r.status === 200 || r.status === 400,
    'search: response time < 2s': (r) => r.timings.duration < 2000,
  });

  if (!success) {
    errorRate.add(1);
  }
}

/**
 * Teardown function - runs once at the end of the test
 */
export function teardown(data) {
  console.log('✅ Smoke Test Complete');
}

/**
 * Handle test summary
 */
export function handleSummary(data) {
  const summary = {
    stdout: formatSummary(data),
  };

  // Add JSON output if environment variable is set
  if (__ENV.EXPORT_JSON) {
    summary[`${__ENV.EXPORT_JSON}`] = JSON.stringify(data, null, 2);
  }

  // Add HTML report if environment variable is set
  if (__ENV.EXPORT_HTML) {
    summary[`${__ENV.EXPORT_HTML}`] = htmlReport(data);
  }

  return summary;
}

/**
 * Format summary for console output
 */
function formatSummary(data) {
  const httpReqs = data.metrics.http_reqs?.values.count || 0;
  const httpReqFailed = data.metrics.http_req_failed?.values.rate || 0;
  const httpReqDuration = data.metrics.http_req_duration?.values || {};

  return `
================================================================================
SMOKE TEST SUMMARY
================================================================================
Total Requests:        ${httpReqs}
Failed Requests:       ${Math.round(httpReqs * httpReqFailed)}
Success Rate:          ${((1 - httpReqFailed) * 100).toFixed(2)}%
Average Duration:      ${(httpReqDuration.avg || 0).toFixed(2)}ms
p95 Duration:          ${(httpReqDuration['p(95)'] || 0).toFixed(2)}ms
p99 Duration:          ${(httpReqDuration['p(99)'] || 0).toFixed(2)}ms
Throughput:            ${(data.metrics.http_reqs?.values.rate || 0).toFixed(2)} req/s
Error Rate:            ${(data.metrics.errors?.values.rate || 0).toFixed(2)}
================================================================================
Test Result: ${httpReqFailed < 0.05 ? '✅ PASSED' : '❌ FAILED'}
================================================================================
`;
}

/**
 * Generate HTML report
 */
function htmlReport(data) {
  // Basic HTML report - can be enhanced with a proper template
  return `<!DOCTYPE html>
<html>
<head>
  <title>k6 Smoke Test Report</title>
  <style>
    body { font-family: Arial, sans-serif; margin: 20px; }
    h1 { color: #333; }
    .metric { margin: 10px 0; }
    .passed { color: green; }
    .failed { color: red; }
  </style>
</head>
<body>
  <h1>k6 Smoke Test Report</h1>
  <p>Generated: ${new Date().toISOString()}</p>
  <div class="metric">Total Requests: ${data.metrics.http_reqs?.values.count || 0}</div>
  <div class="metric">Success Rate: ${((1 - (data.metrics.http_req_failed?.values.rate || 0)) * 100).toFixed(2)}%</div>
  <div class="metric">p95 Duration: ${(data.metrics.http_req_duration?.values['p(95)'] || 0).toFixed(2)}ms</div>
</body>
</html>`;
}
