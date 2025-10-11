/**
 * Spike Test - Sudden Traffic Surge
 *
 * Purpose: Test system behavior under sudden load spike
 * Duration: ~3 minutes
 * Load: 10 → 200 → 10 VUs (instant changes)
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';

const config = JSON.parse(open('../config/default.json'));
const BASE_URL = __ENV.BASE_URL || config.baseUrl;
const users = JSON.parse(open('../fixtures/users.json'));

const errorRate = new Rate('errors');
const spikeRecovery = new Rate('spike_recovery');

export const options = {
  stages: [
    { duration: '30s', target: 10 },    // Normal load
    { duration: '0s', target: 200 },    // Instant spike
    { duration: '1m', target: 200 },    // Sustained spike
    { duration: '0s', target: 10 },     // Instant drop
    { duration: '1m', target: 10 },     // Recovery period
  ],
  thresholds: {
    http_req_duration: ['p(95)<3000'],
    http_req_failed: ['rate<0.05'],     // Allow 5% error during spike
    spike_recovery: ['rate>0.9'],        // System should recover
  },
  tags: {
    test_type: 'spike',
  },
};

export function setup() {
  console.log('📈 Starting Spike Test');
  console.log('   Testing sudden load surge handling...');
  return {
    baseUrl: BASE_URL,
    testUser: users.testUser,
  };
}

export default function (data) {
  const { baseUrl, testUser } = data;

  const token = authenticate(baseUrl, testUser);
  if (!token) {
    errorRate.add(1);
    spikeRecovery.add(0);
    sleep(2);
    return;
  }

  // Health check
  const response = http.get(`${baseUrl}/health`, {
    tags: { name: 'health_check' },
  });

  const success = check(response, {
    'health: status is 200': (r) => r.status === 200,
    'health: response time < 2s': (r) => r.timings.duration < 2000,
  });

  spikeRecovery.add(success ? 1 : 0);
  if (!success) errorRate.add(1);

  sleep(1);
}

function authenticate(baseUrl, user) {
  const response = http.post(
    `${baseUrl}/api/v1/auth/login`,
    JSON.stringify({ email: user.email, password: user.password }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  return response.status === 200 ? response.json('accessToken') : null;
}

export function handleSummary(data) {
  const recoveryRate = data.metrics.spike_recovery?.values.rate || 0;
  const passed = recoveryRate > 0.9;

  return {
    stdout: `
================================================================================
SPIKE TEST SUMMARY
================================================================================
Total Requests:        ${data.metrics.http_reqs?.values.count || 0}
Recovery Rate:         ${(recoveryRate * 100).toFixed(2)}%
p95 Duration:          ${(data.metrics.http_req_duration?.values['p(95)'] || 0).toFixed(2)}ms
Error Rate:            ${((data.metrics.http_req_failed?.values.rate || 0) * 100).toFixed(2)}%
Test Result:           ${passed ? '✅ PASSED' : '❌ FAILED'}
================================================================================
Spike: 10 → 200 VUs instant, system ${passed ? 'recovered successfully' : 'did not recover'}
================================================================================
`,
  };
}
