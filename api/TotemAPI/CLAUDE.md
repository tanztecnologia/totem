# TotemAPI (.NET)

## Stack
- ASP.NET Core + JWT
- EF Core (SQLite) + migrations
- Testes: xUnit

## Como rodar
- `dotnet run` em `api/TotemAPI/TotemAPI`
- Profile padrão: `http://localhost:5120`
- Banco local (SQLite): `totem.local.db` (connection string `LocalDb`)
- Migrations são aplicadas no startup (`db.Database.Migrate()` em `Program.cs`)

## Testes
- `dotnet test` em `api/TotemAPI`

## Convenções e Arquitetura
- Organização por feature (feature-first):
  - `Features/<Feature>/Domain`: entidades e enums (sem infra)
  - `Features/<Feature>/Application`: contratos (abstrações) e use cases
  - `Features/<Feature>/Infrastructure`: repositórios/integrações (EF/HTTP/etc)
  - `Features/<Feature>/Controllers`: endpoints HTTP
- Multitenancy
  - Tenant vem do JWT via claim `tenant_id`
  - Controllers validam `tenant_id` e roles (Admin/Staff/Totem) conforme o endpoint

## Features e Endpoints
- Identity
  - `POST /api/auth/register`
  - `POST /api/auth/login`
  - Claim `tenant_id` no JWT, usado por controllers via `User.FindFirstValue("tenant_id")`
- Authorization (roles)
  - `UserRole.Admin`, `UserRole.Staff`, `UserRole.Totem`
- Catalog (SKUs)
  - `GET /api/skus`
  - `GET /api/skus/{id}`
- Observação sobre SKU Code
  - Code é normalizado e comparado em maiúsculo (`NormalizeCode`).
  - Integração do totem depende do `Code` para mapear itens locais -> SKUs reais.
- Cart
  - `POST /api/carts`
  - `GET /api/carts/{id}`
  - `POST /api/carts/{id}/items` (body: `skuId`, `quantity`)
  - `PUT /api/carts/{id}/items/{skuId}` (body: `quantity`)
  - `DELETE /api/carts/{id}/items` (limpa carrinho)
- Checkout
  - `POST /api/checkout` (body exige `cartId`, `fulfillment`, `paymentMethod`)
  - `POST /api/checkout/payments/{paymentId}/confirm`
  - `GET /api/checkout/orders/{orderId}`

## Regras importantes
- Checkout inicia apenas com `cartId` (fluxo simplificado).
- Ao aprovar pagamento, o carrinho associado ao pedido é limpo.
- Sempre que houver alteração em endpoints (rota/verbo/request/response), atualizar o Postman:
  - `postman/TotemAPI.postman_collection.json`

## TEF (fake vs API)
- Toggle via configuração:
  - `Tef__Mode = "Api"` usa `HttpTefPaymentService`
  - Qualquer outro valor usa `FakeTefPaymentService`

## TEF via ACBrMonitor (diretriz de integração)
- Para cartão (crédito/débito) usando ACBrMonitor como ponte:
  - A ponta (Totem + ACBrMonitor) normalmente obtém o resultado primeiro (aprovado/negado, NSU, autorização).
  - A API deve ser chamada para registrar/confirmar o pagamento do pedido (auditoria e consistência).
- Para Pix:
  - O backend é a fonte de verdade quando houver integração com PSP (webhook); o totem pode receber push (SSE/WebSocket) ou fazer polling como fallback.

## Arquivos de referência
- Bootstrap/DI/Auth/Migrations: `TotemAPI/Program.cs`
- Checkout: `TotemAPI/Features/Checkout/`
- Cart: `TotemAPI/Features/Cart/`
- Identity/JWT: `TotemAPI/Features/Identity/`
