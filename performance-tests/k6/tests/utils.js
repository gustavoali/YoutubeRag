import { check } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';

// Custom metrics
export const errorRate = new Rate('errors');
export const successRate = new Rate('success');
export const authDuration = new Trend('auth_duration');
export const videoIngestionDuration = new Trend('video_ingestion_duration');
export const searchDuration = new Trend('search_duration');
export const requestCounter = new Counter('requests_total');

/**
 * Load configuration from JSON file with environment variable override
 * @param {string} configPath - Path to configuration file
 * @returns {object} Configuration object
 */
export function loadConfig(configPath) {
  const config = JSON.parse(open(configPath));

  // Override with environment variables
  if (__ENV.BASE_URL) {
    config.baseUrl = __ENV.BASE_URL;
  }

  if (__ENV.API_VERSION) {
    config.apiVersion = __ENV.API_VERSION;
  }

  return config;
}

/**
 * Get a random item from an array
 * @param {Array} array - Array to select from
 * @returns {*} Random item from array
 */
export function getRandomItem(array) {
  return array[Math.floor(Math.random() * array.length)];
}

/**
 * Authenticate and return JWT token
 * @param {string} baseUrl - Base URL of API
 * @param {object} user - User credentials
 * @returns {string|null} JWT token or null if authentication failed
 */
export function authenticate(baseUrl, user) {
  const loginUrl = `${baseUrl}/api/v1/auth/login`;
  const payload = JSON.stringify({
    email: user.email,
    password: user.password
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
    tags: { name: 'auth_login' },
  };

  const response = http.post(loginUrl, payload, params);

  const success = check(response, {
    'auth: status is 200': (r) => r.status === 200,
    'auth: has access token': (r) => r.json('accessToken') !== undefined,
  });

  if (success) {
    successRate.add(1);
    return response.json('accessToken');
  } else {
    errorRate.add(1);
    console.error(`Authentication failed: ${response.status} - ${response.body}`);
    return null;
  }
}

/**
 * Create authorization headers with JWT token
 * @param {string} token - JWT token
 * @returns {object} Headers object
 */
export function getAuthHeaders(token) {
  return {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`,
  };
}

/**
 * Standard check for successful HTTP response
 * @param {object} response - HTTP response object
 * @param {string} operationName - Name of operation for metrics
 * @returns {boolean} True if all checks passed
 */
export function checkSuccess(response, operationName) {
  const success = check(response, {
    [`${operationName}: status is 2xx`]: (r) => r.status >= 200 && r.status < 300,
    [`${operationName}: response time OK`]: (r) => r.timings.duration < 3000,
  });

  if (success) {
    successRate.add(1);
  } else {
    errorRate.add(1);
    console.error(`${operationName} failed: ${response.status} - ${response.body}`);
  }

  requestCounter.add(1);
  return success;
}

/**
 * Generate random video URL for testing
 * @returns {string} Random YouTube video URL
 */
export function generateRandomVideoUrl() {
  const videoIds = [
    'dQw4w9WgXcQ',
    '9bZkp7q19f0',
    'kJQP7kiw5Fk',
    'JGwWNGJdvx8',
    'OPf0YbXqDm0'
  ];
  const randomId = getRandomItem(videoIds);
  return `https://www.youtube.com/watch?v=${randomId}`;
}

/**
 * Sleep with random jitter to simulate realistic user behavior
 * @param {number} baseSeconds - Base sleep duration in seconds
 * @param {number} jitterSeconds - Maximum jitter in seconds
 */
export function sleepWithJitter(baseSeconds, jitterSeconds = 1) {
  const jitter = Math.random() * jitterSeconds;
  sleep(baseSeconds + jitter);
}

/**
 * Format test summary for console output
 * @param {object} data - Test summary data
 * @returns {string} Formatted summary
 */
export function formatSummary(data) {
  return `
================================================================================
PERFORMANCE TEST SUMMARY
================================================================================
Total Requests:        ${data.metrics.http_reqs?.values.count || 0}
Failed Requests:       ${data.metrics.http_req_failed?.values.passes || 0}
Success Rate:          ${((1 - (data.metrics.http_req_failed?.values.rate || 0)) * 100).toFixed(2)}%
Average Duration:      ${(data.metrics.http_req_duration?.values.avg || 0).toFixed(2)}ms
p95 Duration:          ${(data.metrics.http_req_duration?.values['p(95)'] || 0).toFixed(2)}ms
p99 Duration:          ${(data.metrics.http_req_duration?.values['p(99)'] || 0).toFixed(2)}ms
Throughput:            ${(data.metrics.http_reqs?.values.rate || 0).toFixed(2)} req/s
Data Received:         ${((data.metrics.data_received?.values.count || 0) / 1024 / 1024).toFixed(2)} MB
Data Sent:             ${((data.metrics.data_sent?.values.count || 0) / 1024).toFixed(2)} KB
================================================================================
`;
}

/**
 * Setup HTML report generation
 * @param {string} testName - Name of the test
 * @returns {object} Options for HTML report
 */
export function setupHtmlReport(testName) {
  return {
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
    summaryTimeUnit: 'ms',
  };
}
