# Observability (Docker Compose)

Este stack sobe:
- Jaeger (OTLP gRPC :4317, UI :16686)
- Prometheus (:9090)
- Loki (:3100)
- Fluent Bit (tail de `./logs/totemapi.jsonl` → Loki)
- Grafana (:3000) com datasource do Loki provisionado

## Subir

```bash
mkdir -p logs
docker compose up -d
```

## Derrubar

```bash
docker compose down
```

## URLs

- Jaeger UI: http://localhost:16686
- Grafana: http://localhost:3000
- Loki: http://localhost:3100
- Prometheus: http://localhost:9090

## Dashboard FinOps (tenants)

No Grafana, existe a pasta `Totem` com o dashboard:
- `FinOps - Tenants (Logs)`
- `FinOps - Tenants (Loki Ingest)`

Ele usa o campo `tenant_id` nos logs para estimar consumo por tenant (volume de logs e erros).

## Loki multi-tenant (FinOps/chargeback)

O Loki está com multi-tenancy habilitado (auth_enabled=true). O Fluent Bit envia o `tenant_id` como `X-Scope-OrgID`, então:
- Cada tenant fica isolado dentro do Loki
- As métricas do próprio Loki passam a expor consumo por tenant (`tenant=...`) para Prometheus/Grafana

No Grafana, a datasource do Loki está provisionada com `X-Scope-OrgID=unknown` (tenant padrão). Para consultar logs de um tenant específico, ajuste o header no datasource (ou crie um datasource por tenant).

## Logs JSON do TotemAPI

Para o Fluent Bit conseguir indexar melhor, o TotemAPI pode escrever logs em JSON lines (um JSON por linha) no arquivo:

`logs/totemapi.jsonl`

Configure no `.env` (ou variáveis de ambiente):

```bash
Otel__Endpoint=http://localhost:4317
Logging__File__Path=logs/totemapi.jsonl
```

## stdout de containers (driver fluentd)

O Fluent Bit também está preparado para receber logs via protocolo Fluentd (porta 24224) e enviar ao Loki.

Exemplo para um serviço no `docker compose`:

```yaml
services:
  totemapi:
    image: totemapi
    logging:
      driver: fluentd
      options:
        fluentd-address: "127.0.0.1:24224"
        tag: "totemapi"
```

Esse `fluentd-address` é resolvido pelo Docker daemon (host). Por isso usamos a porta publicada do Fluent Bit.
