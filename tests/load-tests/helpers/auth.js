import http from 'k6/http';
import { check } from 'k6';
import { BASE_URL, JSON_HEADERS } from '../k6.config.js';

export function login(email, password) {
    const res = http.post(
        `${BASE_URL}/api/auth/login`,
        JSON.stringify({ email, password }),
        { headers: JSON_HEADERS }
    );

    const ok = check(res, { 'login 200': (r) => r.status === 200 });

    if (!ok) {
        console.error(`[auth] Login falhou: ${res.status} — ${res.body?.substring(0, 300)}`);
        return null;
    }

    try {
        const body = JSON.parse(res.body);
        return body.accessToken ?? body.token ?? body.access_token ?? null;
    } catch {
        console.error('[auth] Erro ao parsear resposta de login');
        return null;
    }
}

export function bearerHeaders(token) {
    return {
        'Content-Type':  'application/json',
        'Accept':        'application/json',
        'User-Agent':    'k6-samgestor/2.0',
        'Authorization': `Bearer ${token}`,
    };
}