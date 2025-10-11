/**
 * Search Performance Load Test
 *
 * Purpose: Test semantic search endpoint under load
 * Duration: 3 minutes
 * Load: 100 VUs
 *
 * Thresholds:
 * - p95 < 500ms
 * - p99 < 1s
 * - Error rate < 1%
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const config = JSON.parse(open('../config/default.json'));
const BASE_URL = __ENV.BASE_URL || config.baseUrl;
const users = JSON.parse(open('../fixtures/users.json'));
const searchQueries = JSON.parse(open('../fixtures/search-queries.json'));

const errorRate = new Rate('errors');
const searchDuration = new Trend('search_duration');
const searchSuccess = new Rate('search_success');

export const options = {
  vus: 100,
  duration: '3m',
  thresholds: {
    http_req_duration: ['p(95)<500', 'p(99)<1000'],
    http_req_failed: ['rate<0.01'],
    search_duration: ['p(95)<500', 'p(99)<1000'],
    search_success: ['rate>0.99'],
    errors: ['rate<0.01'],
  },
  tags: {
    test_type: 'load',
    scenario: 'search',
  },
};

export function setup() {
  console.log('🔍 Starting Search Load Test');
  console.log(`   VUs: 100, Duration: 3m`);
  return {
    baseUrl: BASE_URL,
    testUser: users.testUser,
    queries: searchQueries.queries,
  };
}

export default function (data) {
  const { baseUrl, testUser, queries } = data;

  const token = authenticate(baseUrl, testUser);
  if (!token) {
    errorRate.add(1);
    sleep(5);
    return;
  }

  // Perform multiple searches with varying complexity
  for (let i = 0; i < 3; i++) {
    const query = queries[Math.floor(Math.random() * queries.length)];
    performSearch(baseUrl, token, query);
    sleep(Math.random() * 2 + 1);
  }
}

function authenticate(baseUrl, user) {
  const response = http.post(
    `${baseUrl}/api/v1/auth/login`,
    JSON.stringify({ email: user.email, password: user.password }),
    { headers: { 'Content-Type': 'application/json' }, tags: { name: 'auth' } }
  );

  if (check(response, { 'auth success': (r) => r.status === 200 })) {
    return response.json('accessToken');
  }
  return null;
}

function performSearch(baseUrl, token, query) {
  const startTime = Date.now();

  const response = http.post(
    `${baseUrl}/api/v1/search/semantic`,
    JSON.stringify({
      query: query.query,
      maxResults: 10,
      minRelevanceScore: 0.5,
    }),
    {
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
      tags: {
        name: 'search_semantic',
        complexity: query.complexity,
      },
    }
  );

  const duration = Date.now() - startTime;
  searchDuration.add(duration);

  const success = check(response, {
    'search: status is 200 or 400': (r) => r.status === 200 || r.status === 400,
    'search: response time OK': (r) => r.timings.duration < 1000,
  });

  searchSuccess.add(success ? 1 : 0);
  if (!success) errorRate.add(1);
}

export function handleSummary(data) {
  const summary = `
================================================================================
SEARCH LOAD TEST SUMMARY
================================================================================
Total Requests:        ${data.metrics.http_reqs?.values.count || 0}
Success Rate:          ${((data.metrics.search_success?.values.rate || 0) * 100).toFixed(2)}%
p95 Duration:          ${(data.metrics.search_duration?.values['p(95)'] || 0).toFixed(2)}ms
p99 Duration:          ${(data.metrics.search_duration?.values['p(99)'] || 0).toFixed(2)}ms
Throughput:            ${(data.metrics.http_reqs?.values.rate || 0).toFixed(2)} req/s
Test Result: ${(data.metrics.search_success?.values.rate || 0) > 0.99 ? '✅ PASSED' : '❌ FAILED'}
================================================================================
`;

  const result = { stdout: summary };
  if (__ENV.EXPORT_JSON) result[__ENV.EXPORT_JSON] = JSON.stringify(data, null, 2);
  return result;
}
