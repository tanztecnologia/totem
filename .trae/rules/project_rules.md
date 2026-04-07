# Contexto do Projeto

- Projeto: app Flutter (Dart) para Totem/Kiosk de autoatendimento.
- Objetivo de UI: menu lateral com categorias (esquerda) e opções de compra da categoria selecionada (direita).
- Ponto de entrada: lib/main.dart.
- Design System: package local em packages/totem_ds. Toda UI reutilizável deve existir nele e ser consumida pelo app.

# Arquitetura

- Padrão: Clean Architecture com organização por feature (feature-first).
- Local das features: lib/src/features/<feature_name>/.
- Camadas por feature:
  - domain: entidades e contratos (sem Flutter)
  - data: implementações (ex.: repositórios em memória)
  - presentation: páginas, widgets e controllers (Flutter)

# Feature Atual: kiosk

- Caminho: lib/src/features/kiosk/
- Domain:
  - Entidades: KioskCategory e Product
  - Contrato: CatalogRepository
- Data:
  - Implementação: InMemoryCatalogRepository
- Presentation:
  - Estado: KioskCubit + KioskState (Bloc/Cubit)
  - UI: KioskPage (composição) + componentes do DS (TotemTopBar, TotemSideMenu, TotemProductCard, TotemCartPanel, TotemProductDetailSheet, TotemCheckoutDialog, TotemQuantityStepper)

# Convenções

- Evitar o nome Category para entidades (conflito com @Category do Flutter); usar KioskCategory.
- Gerenciamento de estado: flutter_bloc (Cubit + BlocBuilder).
- UI em Material 3.
- Componentes: não criar widgets compartilháveis dentro de features; criar/usar em packages/totem_ds e importar via package:totem_ds/totem_ds.dart.
- Não adicionar comentários no código.
- Toda feature nova deve nascer com testes (mínimo: unit tests de Bloc/UseCases/Services e, quando aplicável, widget tests do fluxo principal).
- Sempre rodar regressivo antes de finalizar uma mudança (flutter analyze + flutter test) e corrigir quebras em testes existentes.
- Sempre que houver alteração no front (Totem/Flutter) que afete payloads/contratos consumidos, equalizar a API (TotemAPI) e validar os testes das duas plataformas (flutter analyze + flutter test + dotnet test).
- Sempre que houver qualquer alteração em endpoints da API (adição/remoção/alteração de rota, verbos, request/response), atualizar a collection do Postman em postman/TotemAPI.postman_collection.json.

# Comandos de Verificação

- flutter analyze
- flutter test
- dotnet test (em api/TotemAPI)
