#!/usr/bin/env bash

echo ">> Derrubando containers..."

container rm -f otel-collector 2>/dev/null || true
container rm -f grafana 2>/dev/null || true
container rm -f loki 2>/dev/null || true
container rm -f jaeger 2>/dev/null || true

echo ">> Removendo network..."
container network rm observability 2>/dev/null || true

rm -f .container_ips .env otel-config.yaml

echo "Tudo removido."