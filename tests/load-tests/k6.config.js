export const BASE_URL    = __ENV.BASE_URL    || 'http://localhost:5000';
export const PAYMENT_URL = __ENV.PAYMENT_URL || 'http://localhost:5002';
export const RETREAT_ID  = __ENV.RETREAT_ID  || '';

export const ADMIN_EMAIL    = __ENV.ADMIN_EMAIL    || 'admin@sam.local';
export const ADMIN_PASSWORD = __ENV.ADMIN_PASSWORD || 'Admin@123';


export const CONTEMPLATION_PATH =
    __ENV.CONTEMPLATION_PATH || '/api/retreats/__RETREAT_ID__/selection/run';

export const JSON_HEADERS = {
    'Content-Type': 'application/json',
    'Accept':       'application/json',
    'User-Agent':   'k6-samgestor/2.0',
};

export const DEFAULT_THRESHOLDS = {
    'http_req_duration': ['p(95)<500', 'p(99)<1500'],
    'http_req_failed':   ['rate<0.01'],
    'checks':            ['rate>0.95'],
};

export function authHeaders(token) {
    return { ...JSON_HEADERS, 'Authorization': `Bearer ${token}` };
}