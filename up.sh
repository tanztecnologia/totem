#!/usr/bin/env bash

set -e

NETWORK="observability"

echo ">> Criando network (se não existir)..."
container network create $NETWORK 2>/dev/null || true

echo ">> Limpando arquivos antigos..."
rm -f .container_ips otel-config.yaml

# -------------------------------------------------------
# Extrai o status do container a partir do JSON do Apple
# Container.app (não suporta --format do Docker).
# Estrutura: [{"status":"running", ...}]
# -------------------------------------------------------
function container_status() {
  container inspect "$1" 2>/dev/null \
    | python3 -c "import sys,json; print(json.load(sys.stdin)[0].get('status',''))" \
    2>/dev/null || echo ""
}

# -------------------------------------------------------
# Extrai o IP real do container a partir do campo
# networks[0].address (formato CIDR: "192.168.x.x/24").
# No Apple Container.app os containers recebem IPs reais
# da rede local, não de uma sub-rede de VM.
# -------------------------------------------------------
function container_ip() {
  container inspect "$1" 2>/dev/null \
    | python3 -c "
import sys, json
d = json.load(sys.stdin)[0]
addr = d['networks'][0]['address']   # ex: '192.168.65.2/24'
print(addr.split('/')[0])
" 2>/dev/null || echo ""
}

# -------------------------------------------------------
# run_container <name> [args...]
# Sobe o container e aguarda status "running" (até 20s).
# -------------------------------------------------------
function run_container() {
  local NAME=$1
  shift

  echo ">> Subindo $NAME..."
  container rm -f "$NAME" 2>/dev/null || true

  container run -d \
    --name "$NAME" \
    --network $NETWORK \
    "$@"

  local MAX=20
  local I=0
  while true; do
    local STATUS
    STATUS=$(container_status "$NAME")
    if [ "$STATUS" = "running" ]; then
      break
    fi
    I=$((I + 1))
    if [ $I -ge $MAX ]; then
      echo "❌ Timeout aguardando $NAME ficar running (último status: '$STATUS')"
      exit 1
    fi
    sleep 1
  done

  local IP
  IP=$(container_ip "$NAME")
  echo ">> $NAME: running ✔  IP=$IP"
  echo "$NAME=$IP" >> .container_ips
}

# -------------------------------------------------------
# Jaeger
# -------------------------------------------------------
run_container jaeger \
  -p 16686:16686 \
  -p 4317:4317 \
  -e COLLECTOR_OTLP_ENABLED=true \
  jaegertracing/all-in-one:latest

# -------------------------------------------------------
# Loki
# -------------------------------------------------------
run_container loki \
  -p 3100:3100 \
  grafana/loki:latest \
  -config.file=/etc/loki/local-config.yaml

# -------------------------------------------------------
# Grafana
# -------------------------------------------------------
run_container grafana \
  -p 3000:3000 \
  -e GF_AUTH_ANONYMOUS_ENABLED=true \
  -e GF_AUTH_ANONYMOUS_ORG_ROLE=Admin \
  grafana/grafana:latest

# -------------------------------------------------------
# Lê o IP real do Jaeger para gerar o .env.
# Apple Container.app dá IPs reais/roteáveis — sem VM.
# -------------------------------------------------------
JAEGER_IP=$(container_ip jaeger)

echo ""
echo ">> Gerando .env.observability..."

cat <<EOF > .env.observability
# OpenTelemetry — API .NET (gRPC, porta padrão 4317)
Otel__Endpoint=http://${JAEGER_IP}:4317

# OpenTelemetry — Flutter
OTEL_EXPORTER_OTLP_ENDPOINT=http://${JAEGER_IP}:4317
EOF

# Atualiza (ou cria) as duas variáveis de OTel no .env principal sem sobrescrever o restante.
if [ -f .env ]; then
  python3 - "${JAEGER_IP}" <<'PYEOF'
import sys, re, pathlib

ip = sys.argv[1]
env_path = pathlib.Path(".env")
lines = env_path.read_text().splitlines(keepends=True)

keys_to_set = {
    "Otel__Endpoint": f"http://{ip}:4317",
    "OTEL_EXPORTER_OTLP_ENDPOINT": f"http://{ip}:4317",
}

updated = set()
new_lines = []
for line in lines:
    matched = False
    for key, val in keys_to_set.items():
        if re.match(rf"^\s*{re.escape(key)}\s*=", line):
            new_lines.append(f"{key}={val}\n")
            updated.add(key)
            matched = True
            break
    if not matched:
        new_lines.append(line)

# Append any keys that weren't already in the file
for key, val in keys_to_set.items():
    if key not in updated:
        new_lines.append(f"{key}={val}\n")

env_path.write_text("".join(new_lines))
PYEOF
else
  cp .env.observability .env
fi

echo ""
echo "Infra pronta 🚀"
echo ""
echo "Jaeger UI:    http://localhost:16686"
echo "Grafana:      http://localhost:3000"
echo "Loki:         http://localhost:3100"
echo ""
echo "OTLP endpoint (API .NET e Flutter):"
echo "  http://${JAEGER_IP}:4317"
echo ""
cat .container_ips
echo ""
echo "Arquivo .env.observability gerado ✔"
echo "Variáveis OTel atualizadas no .env (sem apagar as demais) ✔"
echo ""
echo "Para rodar a API com OTel:"
echo "  cd api/TotemAPI/TotemAPI && dotnet run --Otel:Endpoint=http://${JAEGER_IP}:4317"
