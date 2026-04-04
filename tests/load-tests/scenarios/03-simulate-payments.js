// FASE 3 — Simula webhook de pagamento para todos os selecionados.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';
import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.2/index.js';
import {
    BASE_URL, PAYMENT_URL, RETREAT_ID,
    ADMIN_EMAIL, ADMIN_PASSWORD, DEFAULT_THRESHOLDS, JSON_HEADERS,
} from '../k6.config.js';
import { login, bearerHeaders } from '../helpers/auth.js';

const webhookApproved  = new Counter('webhook_approved');
const webhookErrorRate = new Rate('webhook_error_rate');
const webhookDuration  = new Trend('webhook_duration_ms', true);
const confirmedInCore  = new Counter('confirmed_in_core');

export const options = {
    scenarios: {
        payments: {
            executor:    'shared-iterations',
            vus:         50,
            iterations:  500,   // limitado pelo número real de selecionados
            maxDuration: '5m',
        },
    },
    thresholds: {
        ...DEFAULT_THRESHOLDS,
        'webhook_error_rate':  ['rate<0.01'],
        'webhook_duration_ms': ['p(95)<1000'],
    },
};

// Setup: coleta todos os IDs com status=Selected (paginado) 
export function setup() {
    if (!RETREAT_ID) throw new Error('\n\n⛔ RETREAT_ID obrigatório\n');

    const token = login(ADMIN_EMAIL, ADMIN_PASSWORD);
    const hdrs  = token ? bearerHeaders(token) : JSON_HEADERS;

    console.log(`\n[FASE 3] Coletando inscrições selecionadas para retiro ${RETREAT_ID}...`);

    const selectedIds = [];
    let skip = 0;

    while (true) {
        const url = `${BASE_URL}/api/registrations?retreatId=${RETREAT_ID}&status=Selected&skip=${skip}&take=100`;
        const res = http.get(url, { headers: hdrs });

        if (res.status !== 200) {
            console.error(`[setup] Erro ${res.status}: ${res.body?.substring(0, 300)}`);
            break;
        }

        let items;
        try {
            const page = JSON.parse(res.body);
            items = page.items ?? page.data ?? (Array.isArray(page) ? page : []);
        } catch {
            console.error('[setup] Erro ao parsear lista');
            break;
        }

        if (!items || items.length === 0) break;
        items.forEach(r => { const id = r.id ?? r.registrationId; if (id) selectedIds.push(id); });
        skip += 100;
        if (items.length < 100) break;
    }

    console.log(`[FASE 3] ${selectedIds.length} selecionados encontrados.`);

    if (selectedIds.length === 0) {
        console.warn('[FASE 3] ⚠️ Nenhum selecionado — verifique se a Fase 2 foi executada e aguarde 15s.');
    }

    return { selectedIds, token };
}


export default function ({ selectedIds, token }) {
    if (!selectedIds || selectedIds.length === 0) return;

    const registrationId = selectedIds[__ITER % selectedIds.length];
    if (!registrationId) return;

    // POST /api/dev/payments/{registrationId}/approve
    // SimulatePaymentController: marca como Paid, publica PaymentConfirmedV1
    const url = `${PAYMENT_URL}/api/dev/payments/${registrationId}/approve`;
    const res = http.post(url, null, { headers: JSON_HEADERS, tags: { phase: 'payment_simulate' } });

    webhookDuration.add(res.timings.duration);

    const approved = res.status === 200 || res.status === 204;
    const notFound = res.status === 404; // payment ainda não criado — evento propagando
    const conflict = res.status === 409; // já estava Paid idempotência ok
    const isError  = res.status >= 400 && !notFound && !conflict;

    if (approved) webhookApproved.add(1);
    webhookErrorRate.add(isError ? 1 : 0);

    check(res, {
        '[PAYMENT] 200/204 ou 409 idempotente': (r) =>
            r.status === 200 || r.status === 204 || r.status === 409,
        '[PAYMENT] < 1000ms': (r) => r.timings.duration < 1000,
    });

    if (notFound) {
        // Evento PaymentRequestedV1 ainda em trânsito retry em 1s
        console.warn(`[VU ${__VU}] 404 para ${registrationId} — retry em 1s`);
        sleep(1);
        const retry = http.post(url, null, { headers: JSON_HEADERS });
        if (retry.status === 200 || retry.status === 204) webhookApproved.add(1);
    }

    if (isError) console.error(`[VU ${__VU} ITER ${__ITER}] ${res.status}: ${res.body?.substring(0, 250)}`);

    sleep(0.05 + Math.random() * 0.15);
}

// Teardown valida status finais no Core
export function teardown({ selectedIds, token }) {
    if (!selectedIds || selectedIds.length === 0) return;

    sleep(5); // aguarda PaymentConfirmedV1 ser processado pelo Core

    const hdrs = token ? bearerHeaders(token) : JSON_HEADERS;
    const res  = http.get(
        `${BASE_URL}/api/registrations?retreatId=${RETREAT_ID}&status=PaymentConfirmed&skip=0&take=500`,
        { headers: hdrs }
    );

    if (res.status === 200) {
        try {
            const page  = JSON.parse(res.body);
            const items = page.items ?? page.data ?? (Array.isArray(page) ? page : []);
            confirmedInCore.add(items.length);
            console.log(`[FASE 3] ✅ ${items.length}/${selectedIds.length} com PaymentConfirmed no Core`);
        } catch { console.warn('[FASE 3] Não foi possível parsear validação final'); }
    } else {
        console.warn(`[FASE 3] Validação retornou ${res.status}`);
    }
}

export function handleSummary(data) {
    const m = data.metrics;
    console.log(`
═══════════════════════════════════════════════════
  FASE 3 — RESULTADO DE PAGAMENTOS
═══════════════════════════════════════════════════
  Aprovados:               ${m['webhook_approved']?.values?.count ?? 0}
  Taxa de erro:            ${((m['webhook_error_rate']?.values?.rate ?? 0) * 100).toFixed(2)}%
  p95 webhook:             ${(m['webhook_duration_ms']?.values?.['p(95)'] ?? 0).toFixed(0)}ms
  PaymentConfirmed (Core): ${m['confirmed_in_core']?.values?.count ?? 0}

  📧 MailHog: http://localhost:8025
  📊 Grafana: http://localhost:3000
═══════════════════════════════════════════════════`);

    return {
        stdout: textSummary(data, { indent: '  ', enableColors: true }),
        'tests/load/results/fase3-summary.json': JSON.stringify(data, null, 2),
    };
}