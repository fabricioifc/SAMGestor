// FASE 2 — Contemplação (sorteio) + notificação dos selecionados.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { textSummary } from 'https://jslib.k6.io/k6-summary/0.0.2/index.js';
import {
    BASE_URL, RETREAT_ID, CONTEMPLATION_PATH,
    ADMIN_EMAIL, ADMIN_PASSWORD, JSON_HEADERS,
} from '../k6.config.js';
import { login, bearerHeaders } from '../helpers/auth.js';

export const options = {
    vus:        1,
    iterations: 1,
    thresholds: { 'http_req_failed': ['rate<0.01'] },
};

export function setup() {
    if (!RETREAT_ID) throw new Error('\n\n⛔ RETREAT_ID obrigatório\n');
    const token = login(ADMIN_EMAIL, ADMIN_PASSWORD);
    if (!token) console.warn('[setup] Sem token — endpoints protegidos retornarão 401');
    return { retreatId: RETREAT_ID, token };
}

export default function ({ retreatId, token }) {
    const hdrs = token ? bearerHeaders(token) : JSON_HEADERS;

    //Contemplação
    const contemUrl = `${BASE_URL}${CONTEMPLATION_PATH.replace('__RETREAT_ID__', retreatId)}`;
    console.log(`\n[FASE 2] Contemplando... ${contemUrl}`);

    const contemRes = http.post(contemUrl, JSON.stringify({ retreatId }), { headers: hdrs });

    check(contemRes, {
        '[CONTEMPLAÇÃO] status 2xx': (r) => r.status >= 200 && r.status < 300,
        '[CONTEMPLAÇÃO] < 30s':      (r) => r.timings.duration < 30000,
    });

    if (contemRes.status >= 200 && contemRes.status < 300) {
        try { console.log(`[CONTEMPLAÇÃO] ✅ ${JSON.stringify(JSON.parse(contemRes.body))}`); }
        catch { console.log(`[CONTEMPLAÇÃO] ✅ status ${contemRes.status}`); }
    } else {
        console.error(`[CONTEMPLAÇÃO] ❌ ${contemRes.status}: ${contemRes.body?.substring(0, 400)}`);
    }

    sleep(2);

    // Notificação dos selecionados
    const notifyUrl = `${BASE_URL}/api/retreats/${retreatId}/notify-selected`;
    console.log(`[FASE 2] Notificando... ${notifyUrl}`);

    const notifyRes = http.post(notifyUrl, null, { headers: hdrs });

    check(notifyRes, {
        '[NOTIFICAÇÃO] status 2xx': (r) => r.status >= 200 && r.status < 300,
        '[NOTIFICAÇÃO] < 60s':      (r) => r.timings.duration < 60000,
    });

    if (notifyRes.status >= 200 && notifyRes.status < 300) {
        try { const b = JSON.parse(notifyRes.body); console.log(`[NOTIFICAÇÃO] ✅ count: ${b.count ?? JSON.stringify(b)}`); }
        catch { console.log(`[NOTIFICAÇÃO] ✅ status ${notifyRes.status}`); }
    } else {
        console.error(`[NOTIFICAÇÃO] ❌ ${notifyRes.status}: ${notifyRes.body?.substring(0, 400)}`);
    }

    console.log(`
[FASE 2] ✅ Concluída.
  ⏳ Aguarde 15s para eventos propagarem no RabbitMQ.
  ▶ PRÓXIMO:
    RETREAT_ID=${retreatId} k6 run tests/load/scenarios/03-simulate-payments.js
`);
}

export function handleSummary(data) {
    return { stdout: textSummary(data, { indent: '  ', enableColors: true }) };
}