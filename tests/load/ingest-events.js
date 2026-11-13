// Ingestion load test: 10 batches of 200 events per second for 2 minutes, through the gateway.
// Thresholds fail the run: p99 http_req_duration under 250 ms and no failed requests.
//
//   k6 run tests/load/ingest-events.js \
//     -e GATEWAY_URL=http://localhost:5100 \
//     -e TOKEN_URL=http://localhost:8080/realms/accesshub/protocol/openid-connect/token \
//     -e ADMIN_PASSWORD=<demo-user-password>
//
// setup() registers SITES sites through the Cardholder API (as the admin user, via the dev-only "load-test" client),
// so traffic is spread across sites and stays under the gateway's per-site rate limit.
import http from 'k6/http';
import { check, fail } from 'k6';
import { uuidv4 } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';

const GATEWAY_URL = __ENV.GATEWAY_URL || 'http://localhost:5100';
const TOKEN_URL = __ENV.TOKEN_URL || 'http://localhost:8080/realms/accesshub/protocol/openid-connect/token';
const SITES = Number(__ENV.SITES || 5);
const BATCH_SIZE = 200;
const DOORS_PER_SITE = 4;
const TYPES = ['AccessGranted', 'AccessDenied', 'DoorOpened', 'DoorClosed'];

export const options = {
  scenarios: {
    ingest: {
      executor: 'constant-arrival-rate',
      rate: 10,
      timeUnit: '1s',
      duration: '2m',
      preAllocatedVUs: 20,
      maxVUs: 50,
    },
  },
  thresholds: {
    'http_req_duration{scenario:ingest}': ['p(99)<250'],
    'http_req_failed{scenario:ingest}': ['rate==0'],
    'checks{scenario:ingest}': ['rate==1'],
  },
};

export function setup() {
  const headers = { Authorization: `Bearer ${adminToken()}`, 'Content-Type': 'application/json' };

  const sites = [];
  for (let i = 0; i < SITES; i++) {
    const site = http.post(`${GATEWAY_URL}/api/v1/sites`, JSON.stringify({ name: `Load ${uuidv4().slice(0, 8)}`, timeZoneId: 'UTC' }), { headers });
    if (site.status !== 201) fail(`site registration failed: ${site.status} ${site.body}`);
    sites.push({ siteId: site.json('siteId'), apiKey: site.json('apiKey'), doors: Array.from({ length: DOORS_PER_SITE }, () => uuidv4()) });
  }

  // EventIngestion learns key hashes from SiteRegistered via the outbox and broker; wait until each site is accepted.
  for (const site of sites) {
    let ready = false;
    for (let attempt = 0; attempt < 60 && !ready; attempt++) {
      ready = post(site, [event(site)]).status === 200;
      if (!ready) http.get(`${GATEWAY_URL}/health/live`); // cheap pause between attempts
    }
    if (!ready) fail(`site ${site.siteId} never became accepted by ingestion`);
  }
  return { sites };
}

export default function (data) {
  const site = data.sites[(__ITER + __VU) % data.sites.length];
  const events = Array.from({ length: BATCH_SIZE }, () => event(site));
  const response = post(site, events);
  check(response, {
    'status is 200': (r) => r.status === 200,
    'every event accepted': (r) => r.status === 200 && r.json('accepted') === BATCH_SIZE,
  });
}

// ADMIN_TOKEN lets a containerised k6 use a token issued to the host (the issuer must match what services expect).
function adminToken() {
  if (__ENV.ADMIN_TOKEN) return __ENV.ADMIN_TOKEN;
  const response = http.post(TOKEN_URL, {
    grant_type: 'password',
    client_id: 'load-test',
    username: __ENV.ADMIN_USER || 'admin',
    password: __ENV.ADMIN_PASSWORD || fail('Set ADMIN_PASSWORD (the AppHost demo-user-password parameter) or ADMIN_TOKEN.'),
  });
  if (response.status !== 200) fail(`token request failed: ${response.status} ${response.body}`);
  return response.json('access_token');
}

function event(site) {
  const type = TYPES[Math.floor(Math.random() * TYPES.length)];
  const decision = type === 'AccessGranted' || type === 'AccessDenied';
  return {
    eventId: uuidv4(),
    doorId: site.doors[Math.floor(Math.random() * site.doors.length)],
    type,
    occurredAt: new Date(Date.now() - Math.floor(Math.random() * 60000)).toISOString(),
    // Effectively unique cards: a small card pool at 500 denials/s would trip the repeated-denials rule for thousands
    // of cards at once and turn this into an alarm-storm test instead of an ingestion test.
    cardNumber: decision ? `L${uuidv4().replace(/-/g, '').slice(0, 15).toUpperCase()}` : null,
    denyReason: type === 'AccessDenied' ? 'UnknownCard' : null,
    accessListSequence: decision ? 1 : null,
  };
}

function post(site, events) {
  return http.post(`${GATEWAY_URL}/api/v1/sites/${site.siteId}/events`, JSON.stringify({ events }), {
    headers: { 'Content-Type': 'application/json', 'X-Site-Id': site.siteId, 'X-Api-Key': site.apiKey },
    tags: { name: 'ingest-batch' },
  });
}
