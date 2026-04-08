#!/usr/bin/env bash

set -euo pipefail

GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; BOLD='\033[1m'; NC='\033[0m'
ok()   { echo -e "${GREEN}[✅]${NC} $*"; }
warn() { echo -e "${YELLOW}[⚠️ ]${NC} $*"; }
log()  { echo -e "${CYAN}[pipeline]${NC} $*"; }
die()  { echo -e "\033[0;31m[❌]${NC} $*"; exit 1; }

command -v k6 >/dev/null 2>&1 || die "k6 não encontrado. Instale: brew install k6"

export BASE_URL="${BASE_URL:-http://localhost:5000}"
export PAYMENT_URL="${PAYMENT_URL:-http://localhost:5002}"
export ADMIN_EMAIL="${ADMIN_EMAIL:-admin@sam.local}"
export ADMIN_PASSWORD="${ADMIN_PASSWORD:-Admin@123}"

if [[ -z "${RETREAT_ID:-}" ]]; then
    [[ -f "tests/load/results/fase1-retreat-id.txt" ]] \
        && RETREAT_ID=$(cat tests/load/results/fase1-retreat-id.txt) \
        && warn "Usando RETREAT_ID salvo: ${RETREAT_ID}" \
        || die "RETREAT_ID obrigatório.\nUso: RETREAT_ID=<guid> bash tests/load/run-pipeline.sh"
fi
export RETREAT_ID

mkdir -p tests/load/results
START=$(date +%s)

echo -e "\n${BOLD}════════════════════════════════════════${NC}"
echo -e "${BOLD}  SAMGestor — Load Test Pipeline${NC}"
echo -e "${BOLD}════════════════════════════════════════${NC}"
echo -e "  Core:      ${BASE_URL}"
echo -e "  Payment:   ${PAYMENT_URL}"
echo -e "  RetreatId: ${RETREAT_ID}"
echo -e "${BOLD}════════════════════════════════════════${NC}\n"

# ── FASE 1 
echo -e "\n${BOLD}▶ FASE 1 — 1500 inscrições${NC}"
T=$(date +%s)
k6 run --out json=tests/load/results/fase1.json \
    tests/load/scenarios/01-seed-registrations.js \
    2>&1 | tee tests/load/results/fase1.log || warn "Fase 1 encerrou com código não-zero"
ok "Fase 1 — $(( $(date +%s) - T ))s"

# ── FASE 2 
echo -e "\n${BOLD}▶ FASE 2 — Contemplação + Notificação${NC}"
T=$(date +%s)
k6 run --out json=tests/load/results/fase2.json \
    tests/load/scenarios/02-contemplation-notify.js \
    2>&1 | tee tests/load/results/fase2.log
ok "Fase 2 — $(( $(date +%s) - T ))s"

# ── WAIT
log "Aguardando propagação RabbitMQ (15s)..."
for i in $(seq 15 -1 1); do printf "\r  ⏳ ${i}s... "; sleep 1; done
echo -e "\r  ✅ Pronto.     "

# ── FASE 3 
echo -e "\n${BOLD}▶ FASE 3 — Simulação de pagamentos${NC}"
T=$(date +%s)
k6 run --out json=tests/load/results/fase3.json \
    tests/load/scenarios/03-simulate-payments.js \
    2>&1 | tee tests/load/results/fase3.log || warn "Fase 3 encerrou com código não-zero"
ok "Fase 3 — $(( $(date +%s) - T ))s"

TOTAL=$(( $(date +%s) - START ))
echo -e "\n${BOLD}════════════════════════════════════════${NC}"
echo -e "${BOLD}  CONCLUÍDO — $(( TOTAL / 60 ))m $(( TOTAL % 60 ))s${NC}"
echo -e "  Resultados: tests/load/results/"
echo -e "  MailHog:    http://localhost:8025"
echo -e "  Grafana:    http://localhost:3000"
echo -e "${BOLD}════════════════════════════════════════${NC}"