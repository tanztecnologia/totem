# Totem (Flutter + TotemAPI)

## Objetivo
- Totem/Kiosk de autoatendimento em Flutter (Material 3), com menu lateral de categorias e painel de carrinho/checkout.
- Backend em .NET (TotemAPI) com autenticação JWT, catálogo, carrinho e checkout (integração TEF).

## Regras do Projeto
- Arquitetura no Flutter: Clean Architecture + feature-first em `lib/src/features/<feature>/`.
- UI reutilizável deve ficar em `packages/totem_ds` e ser consumida pelo app.
- Não criar widgets compartilháveis dentro de features; use o DS.
- Sempre que mudar endpoints/contratos, atualizar `postman/TotemAPI.postman_collection.json`.
- Evitar o nome `Category` para entidades (conflito com `@Category` do Flutter); usar `KioskCategory`.

## Comandos de Verificação (obrigatórios antes de finalizar)
- Flutter: `flutter analyze` e `flutter test`
- API: `dotnet test` (em `api/TotemAPI`)

## Estrutura do Repositório
- App Flutter:
  - Entrada: `lib/main.dart`
  - Features principais:
    - Kiosk: `lib/src/features/kiosk/`
    - Checkout: `lib/src/features/checkout/`
  - Design System: `packages/totem_ds`
- API (.NET):
  - Projeto: `api/TotemAPI/TotemAPI`
  - Testes: `api/TotemAPI/TotemAPI.Tests`
  - Collection Postman: `postman/TotemAPI.postman_collection.json`

## Fluxos (Visão Geral)
- Kiosk (UI)
  - Monta carrinho localmente (linhas + quantidades) enquanto o usuário navega e escolhe itens.
  - Abre o diálogo de checkout a partir do painel do carrinho.
- Checkout (integração com API)
  - O checkout na API inicia apenas com `cartId` (sem itens avulsos).
  - No Pix, o backend é a fonte de verdade para aprovação (via provedor/webhook no futuro); o totem pode receber push (SSE/WebSocket) ou fazer polling como fallback.
  - No cartão (TEF), a ponta (Totem/ACBrMonitor) normalmente sabe o resultado primeiro; a API é usada para registrar/confirmar e manter auditoria.

## Fluxo Atual (Carrinho -> Checkout)
- A API exige `cartId` para iniciar checkout: `POST /api/checkout` (não aceita items avulsos).
- O carrinho é limpo na confirmação de pagamento aprovada: `POST /api/checkout/payments/{paymentId}/confirm`.

## Integração Flutter com a API
- O app usa `CheckoutService` como “porta” do domínio do checkout.
  - Implementação fake (default): `FakeCheckoutService`
  - Implementação real: `TotemApiCheckoutService`
- `CheckoutItem` carrega `skuCodes` para mapear SKU Code -> SKU Id via `GET /api/skus` antes de preencher o carrinho na API.
- Estratégia atual do app (modo API)
  - Cria um carrinho na API (`POST /api/carts`).
  - Envia itens agregados por SKU (`POST /api/carts/{cartId}/items`).
  - Inicia checkout via `cartId` (`POST /api/checkout`) e confirma via `paymentId`.
- Dependência importante
  - O catálogo do Kiosk é in-memory (mock) e usa `Sku.id` como “código”.
  - A API espera SKUs reais com `Code` (normalizado em maiúsculo).
  - Para integração consistente, garanta que os `skuCodes` que o Flutter envia existam no `/api/skus` do tenant (mesmo `Code`).

### Rodar Flutter no modo API
- Use `--dart-define` para configurar o backend e credenciais:
  - `TOTEM_API_BASE_URL` (ex.: `http://localhost:5120`)
  - `TOTEM_TENANT_NAME`, `TOTEM_EMAIL`, `TOTEM_PASSWORD`

## Rodar a API localmente
- A profile padrão expõe em `http://localhost:5120` (ver `TotemAPI/Properties/launchSettings.json`).
- Para executar:
  - `dotnet run` em `api/TotemAPI/TotemAPI`

## Referências rápidas de arquivos
- Kiosk:
  - Página principal: `lib/src/features/kiosk/presentation/pages/kiosk_page.dart`
  - Estado do carrinho local: `lib/src/features/kiosk/presentation/bloc/kiosk_state.dart`
- Checkout:
  - Diálogo: `lib/src/features/checkout/presentation/pages/checkout_dialog.dart`
  - Bloc: `lib/src/features/checkout/presentation/bloc/checkout_bloc.dart`
  - Serviço de integração: `lib/src/features/checkout/domain/services/checkout_service.dart`
  - Implementação API: `lib/src/features/checkout/data/services/totem_api_checkout_service.dart`

## Padrões de implementação
- Preferir lógica de negócio em usecases/serviços do domínio, mantendo UI “fina”.
- Evitar adicionar dependências sem necessidade; o app usa `dart:io` para HTTP neste momento.
