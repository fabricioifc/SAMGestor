// FASE 1 — 1000 FAZER + 500 SERVIR em paralelo.
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';
import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.2/index.js';
import { BASE_URL, RETREAT_ID, DEFAULT_THRESHOLDS, JSON_HEADERS } from '../k6.config.js';
import { createRegistrationPayload, createServiceRegistrationPayload } from '../helpers/data-factory.js';

const fazerCreated   = new Counter('fazer_created');
const fazerConflict  = new Counter('fazer_conflict');
const fazerErrorRate = new Rate('fazer_error_rate');
const fazerDuration  = new Trend('fazer_duration_ms', true);

const servirCreated   = new Counter('servir_created');
const servirConflict  = new Counter('servir_conflict');
const servirErrorRate = new Rate('servir_error_rate');
const servirDuration  = new Trend('servir_duration_ms', true);

export const options = {
    scenarios: {
        fazer: {
            executor:    'per-vu-iterations',
            vus:         100,
            iterations:  10,      // 100 × 10 = 1000 exatas
            maxDuration: '5m',
            env: { SCENARIO: 'fazer' },
        },
        servir: {
            executor:    'per-vu-iterations',
            vus:         50,
            iterations:  10,      // 50 × 10 = 500 exatas
            maxDuration: '5m',
            startTime:   '2s',
            env: { SCENARIO: 'servir' },
        },
    },
    thresholds: {
        ...DEFAULT_THRESHOLDS,
        'fazer_error_rate':   ['rate<0.02'],
        'servir_error_rate':  ['rate<0.02'],
        'fazer_duration_ms':  ['p(95)<600'],
        'servir_duration_ms': ['p(95)<600'],
    },
};

export function setup() {
    if (!RETREAT_ID || RETREAT_ID === '') {
        throw new Error(
            '\n\n⛔ RETREAT_ID não definido!\n' +
            'Uso: RETREAT_ID=<guid> k6 run tests/load/scenarios/01-seed-registrations.js\n'
        );
    }
    console.log(`\n▶ RetreatId: ${RETREAT_ID}`);
    console.log(`  FAZER:  100 VUs × 10 iter = 1000 inscrições`);
    console.log(`  SERVIR:  50 VUs × 10 iter =  500 inscrições`);
    return { retreatId: RETREAT_ID };
}

export default function (data) {
    if (__ENV.SCENARIO === 'fazer')  runFazer(data.retreatId);
    if (__ENV.SCENARIO === 'servir') runServir(data.retreatId);
}

function runFazer(retreatId) {
    const payload = createRegistrationPayload(__VU, __ITER, retreatId);
    const res = http.post(
        `${BASE_URL}/api/registrations`,
        JSON.stringify(payload),
        { headers: JSON_HEADERS, tags: { scenario: 'fazer' } }
    );

    fazerDuration.add(res.timings.duration);
    const created  = res.status === 201;
    const conflict = res.status === 409;
    const isError  = res.status >= 400 && !conflict;

    if (created)  fazerCreated.add(1);
    if (conflict) fazerConflict.add(1);
    fazerErrorRate.add(isError ? 1 : 0);

    check(res, {
        '[FAZER] 201 ou 409': (r) => r.status === 201 || r.status === 409,
        '[FAZER] < 600ms':    (r) => r.timings.duration < 600,
    });

    if (isError) console.error(`[FAZER VU=${__VU} ITER=${__ITER}] ${res.status}: ${res.body?.substring(0, 250)}`);
    sleep(0.1 + Math.random() * 0.3);
}

function runServir(retreatId) {
    const payload = createServiceRegistrationPayload(__VU, __ITER, retreatId);
    const res = http.post(
        `${BASE_URL}/api/service-registrations`,
        JSON.stringify(payload),
        { headers: JSON_HEADERS, tags: { scenario: 'servir' } }
    );

    servirDuration.add(res.timings.duration);
    const created  = res.status === 201 || res.status === 200;
    const conflict = res.status === 409;
    const isError  = res.status >= 400 && !conflict;

    if (created)  servirCreated.add(1);
    if (conflict) servirConflict.add(1);
    servirErrorRate.add(isError ? 1 : 0);

    check(res, {
        '[SERVIR] 2xx ou 409': (r) => r.status === 201 || r.status === 200 || r.status === 409,
        '[SERVIR] < 600ms':    (r) => r.timings.duration < 600,
    });

    if (isError) console.error(`[SERVIR VU=${__VU} ITER=${__ITER}] ${res.status}: ${res.body?.substring(0, 250)}`);
    sleep(0.1 + Math.random() * 0.3);
}

export function handleSummary(data) {
    const m = data.metrics;
    console.log(`
═══════════════════════════════════════════════════
  FASE 1 — RESULTADO
═══════════════════════════════════════════════════
  FAZER   criadas: ${m['fazer_created']?.values?.count ?? 0}   p95: ${(m['fazer_duration_ms']?.values?.['p(95)'] ?? 0).toFixed(0)}ms
  SERVIR  criadas: ${m['servir_created']?.values?.count ?? 0}   p95: ${(m['servir_duration_ms']?.values?.['p(95)'] ?? 0).toFixed(0)}ms
  Total HTTP:      ${m['http_reqs']?.values?.count ?? 0}
  RetreatId:       ${RETREAT_ID}

  ▶ PRÓXIMO:
    RETREAT_ID=${RETREAT_ID} k6 run tests/load/scenarios/02-contemplation-notify.js
═══════════════════════════════════════════════════`);

    return {
        stdout: textSummary(data, { indent: '  ', enableColors: true }),
        'tests/load/results/fase1-retreat-id.txt': RETREAT_ID,
    };
}