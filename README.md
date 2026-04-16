# Totem

## Admin Web

```bash
cd admin-web
npm install
npm run dev
```

Variável:
- `NEXT_PUBLIC_API_BASE_URL` (ex.: `http://localhost:5120`)

## Observability (Jaeger + Loki + Fluent Bit + Grafana)

```bash
mkdir -p logs
docker compose up -d
```

URLs:
- Jaeger UI: http://localhost:16686
- Grafana: http://localhost:3000
- Loki: http://localhost:3100
- Prometheus: http://localhost:9090

## Infra + FinOps (multi-tenant)

### Objetivo

Como a solução é white-label, a ideia é conseguir:
- Identificar quais tenants estão gerando mais custo (ex.: volume de logs, ingestão no Loki, erros)
- Aplicar limites por tier (quotas) e evitar que um tenant degrade o ambiente inteiro
- Fazer showback/chargeback por cliente

### Arquitetura (local via Docker Compose; equivalente em k8s)

Stack definido em `compose.yaml`:
- Jaeger (traces via OTLP gRPC)
- Loki (logs, multi-tenant)
- Fluent Bit (ingest de logs e envio ao Loki)
- Prometheus (métricas do Loki e do TotemAPI)
- Grafana (dashboards e exploração)

Fluxos principais:
- **Traces:** TotemAPI → OTLP → Jaeger
- **Logs (stdout):** TotemAPI (stdout JSON) → Docker logging driver (fluentd) → Fluent Bit (forward) → Loki
- **Métricas:** Prometheus faz scrape do TotemAPI (`/metrics`) e do Loki (`/metrics`)

### Multi-tenancy no Loki (isolamento real)

O Loki roda com `auth_enabled=true` e usa o header `X-Scope-OrgID` para separar tenants.

Por que isso ajuda no FinOps:
- Cada tenant vira um “namespace” isolado dentro do Loki
- Você consegue definir overrides/quotas por tenant
- As métricas do próprio Loki passam a expor consumo por tenant (`tenant=...`) para Prometheus/Grafana

Config:
- Loki: `observability/loki/loki.yaml`
- Overrides (ex.: ingestion rate por tenant): `observability/loki/runtime.yaml`

### Como o tenant chega no Loki

O TotemAPI inclui `tenant_id` (e `user_id`) como campo top-level no log JSON.
O Fluent Bit lê esse campo e envia para o Loki usando `tenant_id_key tenant_id`, que vira `X-Scope-OrgID`.

Isso significa:
- Logs com `tenant_id="abc"` vão para o tenant `abc` no Loki
- Logs sem tenant ou `tenant_id="-"` viram `unknown` (tenant padrão)

### Dashboards FinOps

No Grafana (http://localhost:3000) existe a pasta `Totem` com dashboards:
- `FinOps - Tenants (Logs)` (volume e erros por tenant via Loki)
- `FinOps - Tenants (Loki Ingest)` (bytes/linhas/streams por tenant via Prometheus)

### Rodar localmente

```bash
mkdir -p logs data
docker compose up -d --build
```

### Variáveis úteis para o TotemAPI (host)

Para rodar a API fora do compose e ainda assim integrar com a infra:

```bash
Otel__Endpoint=http://localhost:4317
Logging__File__Path=logs/totemapi.jsonl
```

### Observações importantes (k8s)

Em Kubernetes, a estratégia mais comum é:
- stdout → Fluent Bit/Promtail daemonset → Loki (multi-tenant por header)
- Prometheus scrape (kubelet/service/ingress) + métricas de Loki
- Grafana com dashboards por tenant e alertas por quota

Neste repo já deixamos o padrão principal pronto: **identidade de tenant nos logs** e **separação no Loki**.

Para o TotemAPI escrever logs em JSON lines e o Fluent Bit enviar ao Loki:

```bash
Otel__Endpoint=http://localhost:4317
Logging__File__Path=logs/totemapi.jsonl
```
