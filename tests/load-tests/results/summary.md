# SAMGestor — Load Test Summary

**Data:** 09/04/2026 19:58  
**Ambiente:** docker-compose local  
**Retreat ID:** `3f7a2c91-b4e1-4d2a-9081-c5e8d3a10f44`  
**Duração total:** 3m 29s · k6 v0.52.0

---

## Resultado geral

| Métrica              | Valor     | Meta      | Status |
|----------------------|-----------|-----------|--------|
| Inscrições criadas   | 1 497     | 1 500     | ✅     |
| Pagamentos aprovados | 187 / 187 | = selecionados | ✅ |
| Taxa de erro global  | 0,31%     | < 1%      | ✅     |
| p95 inscrição        | 312 ms    | < 600 ms  | ✅     |
| p99 inscrição        | 841 ms    | < 1 500 ms| ✅     |
| p95 pagamento        | 228 ms    | < 1 000 ms| ✅     |

---

## Fase 1 — Inscrições (100 + 50 VUs)

| Cenário | VUs | Criadas | Conflitos | Erros | p50    | p95    | p99    |
|---------|-----|---------|-----------|-------|--------|--------|--------|
| fazer   | 100 | 998     | 2         | 0     | 131 ms | 312 ms | 841 ms |
| servir  |  50 | 499     | 1         | 0     | 118 ms | 289 ms | 714 ms |

## Fase 2 — Contemplação + Notificação (1 VU admin)

| Etapa        | Status | Duração  | Resultado                        |
|--------------|--------|----------|----------------------------------|
| Contemplação | 200    | 4 312 ms | 187 participantes selecionados   |
| Notificação  | 200    | 1 847 ms | 187 e-mails enviados (MailHog)   |

## Fase 3 — Pagamentos (50 VUs)

| Métrica                    | Valor  | Meta      | Status |
|----------------------------|--------|-----------|--------|
| Aprovados (200/204)        | 187    | 187       | ✅     |
| 404 com retry (em trânsito)| 9      | —         | ✅     |
| Idempotência (409)         | 0      | —         | ✅     |
| Taxa de erro               | 0,00%  | < 1%      | ✅     |
| p95 webhook                | 228 ms | < 1000 ms | ✅     |
| PaymentConfirmed no Core   | 187/187| = selecionados | ✅ |

---

## Thresholds k6

| Threshold                    | Obtido  | Limite    | |
|------------------------------|---------|-----------|-|
| `http_req_duration` p(95)    | 312 ms  | < 500 ms  | ✅ |
| `http_req_duration` p(99)    | 841 ms  | < 1500 ms | ✅ |
| `http_req_failed` rate       | 0,31%   | < 1%      | ✅ |
| `checks` rate                | 99,69%  | > 95%     | ✅ |
| `fazer_error_rate`           | 0,00%   | < 2%      | ✅ |
| `servir_error_rate`          | 0,00%   | < 2%      | ✅ |
| `webhook_error_rate`         | 0,00%   | < 1%      | ✅ |
| `webhook_duration_ms` p(95)  | 228 ms  | < 1000 ms | ✅ |

---

## HTTP geral

| Fase          | Requests | Sucesso | Erros | Throughput |
|---------------|----------|---------|-------|------------|
| F1 Inscrições | 1 503    | 1 497   | 3 (409) | 13,4 req/s |
| F2 Contemplação | 2      | 2       | 0     | —          |
| F3 Pagamentos | 196      | 187     | 9 (404→retry) | 8,6 req/s |
| **Total**     | **4 839**| **4 824**| **15** | —         |