// k6 load test. Run twice against the same environment: Risk__Mode=Off, then Risk__Mode=Hybrid.
// NFR-1 is the DIFFERENCE in p95 between the two runs (added latency from risk), not the absolute number.
//   k6 run -e BASE=https://staging.example -e TOKEN=<jwt> -e ACCOUNTS=id1,id2,... tests/load/payments.js
// Seed enough accounts with large balances first. Thresholds below are placeholders: set them from NFR-1 and NFR-4.
import http from 'k6/http';
import { check } from 'k6';
import { uuidv4 } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

const accounts = (__ENV.ACCOUNTS || '').split(',').filter(Boolean);
export const options = {
  scenarios: {
    ramp: {
      executor: 'ramping-arrival-rate', startRate: 20, timeUnit: '1s', preAllocatedVUs: 200, maxVUs: 1000,
      stages: [{ target: 100, duration: '2m' }, { target: 200, duration: '5m' }, { target: 0, duration: '1m' }],
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],
    'http_req_duration{name:create}': ['p(95)<600', 'p(99)<1200'],
    checks: ['rate>0.99'],
  },
};

export default function () {
  const res = http.post(`${__ENV.BASE}/api/payments`,
    JSON.stringify({ accountId: accounts[Math.floor(Math.random() * accounts.length)], amount: 10 + Math.floor(Math.random() * 500), currency: 'INR', merchantId: 'shop-' + (1 + Math.floor(Math.random() * 20)) }),
    { headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${__ENV.TOKEN}`, 'Idempotency-Key': uuidv4() }, tags: { name: 'create' } });
  check(res, { 'accepted or declined': (r) => r.status === 202 || r.status === 422 });
}
