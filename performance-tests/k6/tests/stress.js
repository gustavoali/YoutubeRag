/**
 * Stress Test - Find System Breaking Point
 *
 * Purpose: Determine system capacity and breaking point
 * Duration: 15 minutes
 * Load: 0 → 200 VUs gradually
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const config = JSON.parse(open('../config/default.json'));
const BASE_URL = __ENV.BASE_URL || config.baseUrl;
const users = JSON.parse(open('../fixtures/users.json'));
const videos = JSON.parse(open('../fixtures/test-videos.json'));

const errorRate = new Rate('errors');

export const options = {
  stages: [
    { duration: '5m', target: 200 },   // Ramp up to 200 VUs
    { duration: '10m', target: 200 },  // Sustain 200 VUs
  ],
  thresholds: {
    http_req_duration: ['p(95)<5000'],  // Relaxed threshold to monitor degradation
    http_req_failed: ['rate<0.1'],       // Allow up to 10% errors to find limits
  },
  tags: {
    test_type: 'stress',
  },
};

export function setup() {
  console.log('⚡ Starting Stress Test');
  console.log('   Finding system breaking point...');
  return {
    baseUrl: BASE_URL,
    testUser: users.testUser,
    videos: videos.videos,
  };
}

export default function (data) {
  const { baseUrl, testUser, videos } = data;

  const token = authenticate(baseUrl, testUser);
  if (!token) {
    errorRate.add(1);
    sleep(5);
    return;
  }

  // Mix of operations
  testVideoList(baseUrl, token);
  sleep(1);

  const video = videos[Math.floor(Math.random() * videos.length)];
  testVideoIngest(baseUrl, token, video);
  sleep(2);
}

function authenticate(baseUrl, user) {
  const response = http.post(
    `${baseUrl}/api/v1/auth/login`,
    JSON.stringify({ email: user.email, password: user.password }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  if (response.status === 200) {
    return response.json('accessToken');
  }
  return null;
}

function testVideoList(baseUrl, token) {
  const response = http.get(`${baseUrl}/api/v1/videos`, {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
  });

  check(response, {
    'video list: status is 2xx': (r) => r.status >= 200 && r.status < 300,
  });
}

function testVideoIngest(baseUrl, token, video) {
  const response = http.post(
    `${baseUrl}/api/v1/videos/ingest`,
    JSON.stringify({
      url: video.url,
      title: video.title,
    }),
    {
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
      },
    }
  );

  check(response, {
    'video ingest: status is 200 or 409': (r) => r.status === 200 || r.status === 409,
  });
}

export function handleSummary(data) {
  const degradationPoint = findDegradationPoint(data);

  return {
    stdout: `
================================================================================
STRESS TEST SUMMARY
================================================================================
Total Requests:        ${data.metrics.http_reqs?.values.count || 0}
Failed Requests:       ${Math.round((data.metrics.http_reqs?.values.count || 0) * (data.metrics.http_req_failed?.values.rate || 0))}
Success Rate:          ${((1 - (data.metrics.http_req_failed?.values.rate || 0)) * 100).toFixed(2)}%
p95 Duration:          ${(data.metrics.http_req_duration?.values['p(95)'] || 0).toFixed(2)}ms
p99 Duration:          ${(data.metrics.http_req_duration?.values['p(99)'] || 0).toFixed(2)}ms
Max VUs Reached:       200
Degradation Started:   ${degradationPoint}
================================================================================
`,
  };
}

function findDegradationPoint(data) {
  const p95 = data.metrics.http_req_duration?.values['p(95)'] || 0;
  if (p95 > 5000) return 'Above 5s response time';
  if (p95 > 3000) return 'Above 3s response time';
  return 'No significant degradation detected';
}
