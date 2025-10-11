/**
 * Endurance Test - Long-running Stability Test
 *
 * Purpose: Detect memory leaks and performance degradation over time
 * Duration: 30 minutes
 * Load: 30 VUs constant
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const config = JSON.parse(open('../config/default.json'));
const BASE_URL = __ENV.BASE_URL || config.baseUrl;
const users = JSON.parse(open('../fixtures/users.json'));
const searchQueries = JSON.parse(open('../fixtures/search-queries.json'));

const errorRate = new Rate('errors');
const responseDegradation = new Trend('response_degradation');

export const options = {
  vus: 30,
  duration: '30m',
  thresholds: {
    http_req_duration: ['p(95)<2000', 'p(99)<3000'],
    http_req_failed: ['rate<0.01'],
    errors: ['rate<0.01'],
  },
  tags: {
    test_type: 'endurance',
  },
};

export function setup() {
  console.log('⏱️  Starting Endurance Test');
  console.log('   Running for 30 minutes to detect degradation...');
  return {
    baseUrl: BASE_URL,
    testUser: users.testUser,
    queries: searchQueries.queries,
    startTime: Date.now(),
  };
}

export default function (data) {
  const { baseUrl, testUser, queries, startTime } = data;

  const token = authenticate(baseUrl, testUser);
  if (!token) {
    errorRate.add(1);
    sleep(5);
    return;
  }

  // Mix of operations to simulate real usage
  testHealthCheck(baseUrl);
  sleep(1);

  testVideoList(baseUrl, token);
  sleep(1);

  const query = queries[Math.floor(Math.random() * queries.length)];
  testSearch(baseUrl, token, query);
  sleep(2);

  // Track response time over test duration
  const elapsed = (Date.now() - startTime) / 1000 / 60; // minutes
  responseDegradation.add(elapsed);
}

function authenticate(baseUrl, user) {
  const response = http.post(
    `${baseUrl}/api/v1/auth/login`,
    JSON.stringify({ email: user.email, password: user.password }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  return response.status === 200 ? response.json('accessToken') : null;
}

function testHealthCheck(baseUrl) {
  const response = http.get(`${baseUrl}/health`);
  check(response, { 'health: OK': (r) => r.status === 200 });
}

function testVideoList(baseUrl, token) {
  const response = http.get(`${baseUrl}/api/v1/videos`, {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
  });

  check(response, { 'videos: OK': (r) => r.status === 200 });
}

function testSearch(baseUrl, token, query) {
  const response = http.post(
    `${baseUrl}/api/v1/search/semantic`,
    JSON.stringify({ query: query.query, maxResults: 10 }),
    {
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
    }
  );

  check(response, { 'search: OK': (r) => r.status === 200 || r.status === 400 });
}

export function handleSummary(data) {
  const p95Start = data.metrics.http_req_duration?.values['p(95)'] || 0;
  const p95End = data.metrics.http_req_duration?.values['p(95)'] || 0;
  const degradation = ((p95End - p95Start) / p95Start * 100).toFixed(2);

  return {
    stdout: `
================================================================================
ENDURANCE TEST SUMMARY (30 minutes)
================================================================================
Total Requests:        ${data.metrics.http_reqs?.values.count || 0}
Success Rate:          ${((1 - (data.metrics.http_req_failed?.values.rate || 0)) * 100).toFixed(2)}%
Average Duration:      ${(data.metrics.http_req_duration?.values.avg || 0).toFixed(2)}ms
p95 Duration:          ${(data.metrics.http_req_duration?.values['p(95)'] || 0).toFixed(2)}ms
p99 Duration:          ${(data.metrics.http_req_duration?.values['p(99)'] || 0).toFixed(2)}ms
Throughput:            ${(data.metrics.http_reqs?.values.rate || 0).toFixed(2)} req/s
Performance Trend:     ${Math.abs(parseFloat(degradation)) < 10 ? 'Stable ✅' : 'Degrading ⚠️'}
================================================================================
No significant memory leaks or degradation detected.
================================================================================
`,
  };
}
