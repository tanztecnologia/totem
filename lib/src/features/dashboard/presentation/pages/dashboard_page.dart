import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../checkout/domain/entities/checkout_order.dart';
import '../../../identity/presentation/bloc/auth_cubit.dart';
import '../../domain/entities/dashboard_order.dart';
import '../../domain/entities/dashboard_overview.dart';
import '../../domain/repositories/catalog_admin_repository.dart';
import '../bloc/dashboard_cubit.dart';
import '../bloc/dashboard_state.dart';

enum _DashboardSection {
  orders,
  revenue,
  createCategory,
  createSku,
  findSku,
  products,
}

class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key});

  @override
  State<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends State<DashboardPage> {
  _DashboardSection _section = _DashboardSection.orders;

  @override
  void initState() {
    super.initState();
    Future<void>.microtask(() async {
      if (!mounted) return;
      await context.read<DashboardCubit>().refreshAll();
    });
  }

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<DashboardCubit, DashboardState>(
      listener: (context, state) {
        final msg = state.errorMessage;
        if (msg != null && msg.trim().isNotEmpty) {
          ScaffoldMessenger.of(context).clearSnackBars();
          ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(msg)));
        }
      },
      builder: (context, state) {
        final cubit = context.read<DashboardCubit>();
        final overview = state.overview;
        final orders = state.orders.toList(growable: false)
          ..sort((a, b) => b.updatedAt.compareTo(a.updatedAt));

        final content = switch (_section) {
          _DashboardSection.orders => _OrdersContent(
              state: state,
              orders: orders,
            ),
          _DashboardSection.revenue => _RevenueContent(
              state: state,
              overview: overview,
            ),
          _DashboardSection.createCategory => _CreateCategoryContent(state: state),
          _DashboardSection.createSku => _CreateSkuContent(state: state),
          _DashboardSection.findSku => _FindSkuContent(state: state),
          _DashboardSection.products => _ProductsContent(state: state),
        };

        return Scaffold(
          appBar: AppBar(
            title: const Text('Dashboard'),
            actions: [
              IconButton(
                onPressed: state.isLoading ? null : () async => _pickDateRange(context, state, cubit),
                icon: const Icon(Icons.date_range),
              ),
              IconButton(
                onPressed: state.isLoading ? null : () => cubit.refreshAll(),
                icon: const Icon(Icons.refresh),
              ),
              IconButton(
                onPressed: () => context.read<AuthCubit>().logout(),
                icon: const Icon(Icons.logout),
              ),
            ],
          ),
          body: SafeArea(
            child: LayoutBuilder(
              builder: (context, constraints) {
                final isWide = constraints.maxWidth >= 900;
                final menu = _DashboardMenu(
                  selected: _section,
                  onSelected: (next) => setState(() => _section = next),
                );

                final right = RefreshIndicator(
                  onRefresh: cubit.refreshAll,
                  child: content,
                );

                if (isWide) {
                  return Row(
                    children: [
                      SizedBox(width: 240, child: menu),
                      const VerticalDivider(width: 1),
                      Expanded(child: right),
                    ],
                  );
                }

                return Scaffold(
                  drawer: Drawer(child: SafeArea(child: menu)),
                  body: right,
                );
              },
            ),
          ),
        );
      },
    );
  }
}

class _DashboardMenu extends StatelessWidget {
  const _DashboardMenu({
    required this.selected,
    required this.onSelected,
  });

  final _DashboardSection selected;
  final ValueChanged<_DashboardSection> onSelected;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Theme.of(context).colorScheme.surface,
      child: ListView(
        padding: const EdgeInsets.all(12),
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
            child: Text(
              'Menu',
              style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w900),
            ),
          ),
          const SizedBox(height: 8),
          _MenuButton(
            icon: Icons.receipt_long_outlined,
            label: 'Ver pedidos',
            selected: selected == _DashboardSection.orders,
            onTap: () => onSelected(_DashboardSection.orders),
          ),
          const SizedBox(height: 8),
          _MenuButton(
            icon: Icons.payments_outlined,
            label: 'Faturamento',
            selected: selected == _DashboardSection.revenue,
            onTap: () => onSelected(_DashboardSection.revenue),
          ),
          const SizedBox(height: 16),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
            child: Text(
              'Cadastros',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w900),
            ),
          ),
          const SizedBox(height: 8),
          _MenuButton(
            icon: Icons.category_outlined,
            label: 'Cadastrar categoria',
            selected: selected == _DashboardSection.createCategory,
            onTap: () => onSelected(_DashboardSection.createCategory),
          ),
          const SizedBox(height: 8),
          _MenuButton(
            icon: Icons.inventory_2_outlined,
            label: 'Cadastrar SKU',
            selected: selected == _DashboardSection.createSku,
            onTap: () => onSelected(_DashboardSection.createSku),
          ),
          const SizedBox(height: 8),
          _MenuButton(
            icon: Icons.search,
            label: 'Buscar SKU',
            selected: selected == _DashboardSection.findSku,
            onTap: () => onSelected(_DashboardSection.findSku),
          ),
          const SizedBox(height: 8),
          _MenuButton(
            icon: Icons.inventory_outlined,
            label: 'Produtos',
            selected: selected == _DashboardSection.products,
            onTap: () => onSelected(_DashboardSection.products),
          ),
        ],
      ),
    );
  }
}

class _MenuButton extends StatelessWidget {
  const _MenuButton({
    required this.icon,
    required this.label,
    required this.selected,
    required this.onTap,
  });

  final IconData icon;
  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final bg = selected ? colorScheme.primaryContainer : colorScheme.surfaceContainerHighest;
    final fg = selected ? colorScheme.onPrimaryContainer : colorScheme.onSurface;

    return Material(
      color: bg,
      borderRadius: BorderRadius.circular(12),
      child: InkWell(
        onTap: () {
          onTap();
          final scaffold = Scaffold.maybeOf(context);
          if (scaffold != null && scaffold.hasDrawer) {
            Navigator.of(context).pop();
          }
        },
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
          child: Row(
            children: [
              Icon(icon, color: fg),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  label,
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(color: fg, fontWeight: FontWeight.w800),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _OrdersContent extends StatelessWidget {
  const _OrdersContent({
    required this.state,
    required this.orders,
  });

  final DashboardState state;
  final List<DashboardOrder> orders;

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<DashboardCubit>();
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _DateRangeHeader(from: state.fromInclusive, to: state.toInclusive),
        const SizedBox(height: 12),
        _BreakdownCard(
          title: 'Pedidos',
          trailing: state.isLoadingOrders
              ? const SizedBox(
                  height: 18,
                  width: 18,
                  child: CircularProgressIndicator(strokeWidth: 2),
                )
              : null,
          child: orders.isEmpty
              ? const Padding(
                  padding: EdgeInsets.symmetric(vertical: 8),
                  child: Text('Nenhum pedido encontrado.'),
                )
              : Column(
                  children: [
                    _RecentOrdersList(orders: orders),
                    if (state.canLoadMore) ...[
                      const SizedBox(height: 12),
                      FilledButton(
                        onPressed: state.isLoadingMoreOrders ? null : () => cubit.loadMoreOrders(),
                        child: state.isLoadingMoreOrders
                            ? const SizedBox(
                                height: 18,
                                width: 18,
                                child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                              )
                            : const Text('Carregar mais'),
                      ),
                    ],
                  ],
                ),
        ),
      ],
    );
  }
}

class _RevenueContent extends StatelessWidget {
  const _RevenueContent({
    required this.state,
    required this.overview,
  });

  final DashboardState state;
  final DashboardOverview? overview;

  @override
  Widget build(BuildContext context) {
    final o = overview;
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _DateRangeHeader(from: state.fromInclusive, to: state.toInclusive),
        const SizedBox(height: 12),
        if (o == null)
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  if (state.isLoadingOverview)
                    const SizedBox(
                      height: 18,
                      width: 18,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  else
                    const Icon(Icons.insights_outlined),
                  const SizedBox(width: 12),
                  const Expanded(child: Text('Carregando visão geral...')),
                ],
              ),
            ),
          )
        else ...[
          _KpiGrid(overview: o),
          const SizedBox(height: 12),
          _BreakdownCard(
            title: 'Pagamentos por método',
            child: _PaymentsByMethodList(items: o.paymentsByMethod),
          ),
          const SizedBox(height: 12),
          _BreakdownCard(
            title: 'Faturamento por tipo',
            child: _PaymentsByProviderList(items: o.paymentsByProvider),
          ),
          const SizedBox(height: 12),
          _BreakdownCard(
            title: 'Pedidos por status (cozinha)',
            child: _KitchenStatusList(items: o.ordersByKitchenStatus),
          ),
        ],
      ],
    );
  }
}

class _CreateCategoryContent extends StatelessWidget {
  const _CreateCategoryContent({
    required this.state,
  });

  final DashboardState state;

  @override
  Widget build(BuildContext context) {
    return _CreateCategoryForm(state: state);
  }
}

class _CreateCategoryForm extends StatefulWidget {
  const _CreateCategoryForm({
    required this.state,
  });

  final DashboardState state;

  @override
  State<_CreateCategoryForm> createState() => _CreateCategoryFormState();
}

class _CreateCategoryFormState extends State<_CreateCategoryForm> {
  late final TextEditingController _categorySlugController;
  late final TextEditingController _categoryNameController;

  bool _isSubmitting = false;

  @override
  void initState() {
    super.initState();
    _categorySlugController = TextEditingController();
    _categoryNameController = TextEditingController();
  }

  @override
  void dispose() {
    _categorySlugController.dispose();
    _categoryNameController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final slugPreview = _normalizeCategoryId(_categorySlugController.text);

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _DateRangeHeader(from: widget.state.fromInclusive, to: widget.state.toInclusive),
        const SizedBox(height: 12),
        Card(
          clipBehavior: Clip.antiAlias,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Cadastrar categoria',
                  style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w900),
                ),
                const SizedBox(height: 8),
                const Text('O código da categoria é gerado automaticamente: 00001, 00002, 00003...'),
                const SizedBox(height: 16),
                TextField(
                  enabled: !_isSubmitting,
                  controller: _categorySlugController,
                  decoration: const InputDecoration(
                    labelText: 'Slug (opcional)',
                    hintText: 'ex.: drinks',
                    border: OutlineInputBorder(),
                  ),
                  onChanged: (_) => setState(() {}),
                ),
                const SizedBox(height: 12),
                TextField(
                  enabled: !_isSubmitting,
                  controller: _categoryNameController,
                  decoration: const InputDecoration(
                    labelText: 'Nome',
                    hintText: 'ex.: Bebidas',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
                _PreviewRow(label: 'Slug', value: slugPreview.isEmpty ? '-' : slugPreview),
                const SizedBox(height: 16),
                FilledButton(
                  onPressed: _isSubmitting ? null : () => _submit(context),
                  child: _isSubmitting
                      ? const SizedBox(
                          height: 18,
                          width: 18,
                          child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                        )
                      : const Text('Salvar'),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Future<void> _submit(BuildContext context) async {
    final repo = context.read<CatalogAdminRepository>();
    final name = _categoryNameController.text.trim();
    final slug = _categorySlugController.text.trim().isEmpty ? null : _categorySlugController.text.trim();

    if (name.length < 2) {
      _showSnack(context, 'Informe um nome válido.');
      return;
    }

    setState(() => _isSubmitting = true);
    try {
      final created = await repo.createCategory(name: name, slug: slug, isActive: true);
      if (!context.mounted) return;
      _showSnack(context, 'Categoria cadastrada: ${created.code} • ${created.name}');
      _categorySlugController.clear();
      _categoryNameController.clear();
      setState(() {});
    } catch (e) {
      if (!context.mounted) return;
      _showSnack(context, e.toString());
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }
}

class _CreateSkuContent extends StatelessWidget {
  const _CreateSkuContent({
    required this.state,
  });

  final DashboardState state;

  @override
  Widget build(BuildContext context) {
    return _CreateSkuForm(
      title: 'Cadastrar SKU',
      subtitle: 'O código do SKU é gerado automaticamente (00001, 00002...).',
      state: state,
    );
  }
}

class _FindSkuContent extends StatefulWidget {
  const _FindSkuContent({
    required this.state,
  });

  final DashboardState state;

  @override
  State<_FindSkuContent> createState() => _FindSkuContentState();
}

class _FindSkuContentState extends State<_FindSkuContent> {
  late final TextEditingController _codeController;
  bool _isLoading = false;
  CatalogAdminSkuResult? _result;

  @override
  void initState() {
    super.initState();
    _codeController = TextEditingController();
  }

  @override
  void dispose() {
    _codeController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _DateRangeHeader(from: widget.state.fromInclusive, to: widget.state.toInclusive),
        const SizedBox(height: 12),
        Card(
          clipBehavior: Clip.antiAlias,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Buscar SKU', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w900)),
                const SizedBox(height: 12),
                TextField(
                  enabled: !_isLoading,
                  controller: _codeController,
                  decoration: const InputDecoration(
                    labelText: 'Código do SKU',
                    hintText: 'ex.: drinks-COCA',
                    border: OutlineInputBorder(),
                  ),
                  onSubmitted: (_) => _search(context),
                ),
                const SizedBox(height: 12),
                FilledButton(
                  onPressed: _isLoading ? null : () => _search(context),
                  child: _isLoading
                      ? const SizedBox(
                          height: 18,
                          width: 18,
                          child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                        )
                      : const Text('Buscar'),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),
        if (_result != null) _SkuDetailsCard(result: _result!),
      ],
    );
  }

  Future<void> _search(BuildContext context) async {
    final repo = context.read<CatalogAdminRepository>();
    final code = _codeController.text.trim();
    if (code.isEmpty) {
      _showSnack(context, 'Informe o código do SKU.');
      return;
    }

    setState(() {
      _isLoading = true;
      _result = null;
    });
    try {
      final result = await repo.getSkuByCode(code: code);
      if (!context.mounted) return;
      if (result == null) {
        _showSnack(context, 'SKU não encontrado.');
        setState(() => _isLoading = false);
        return;
      }
      setState(() {
        _isLoading = false;
        _result = result;
      });
    } catch (e) {
      if (!context.mounted) return;
      _showSnack(context, e.toString());
      setState(() => _isLoading = false);
    }
  }
}

class _SkuDetailsCard extends StatelessWidget {
  const _SkuDetailsCard({required this.result});

  final CatalogAdminSkuResult result;

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Detalhes', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w900)),
            const SizedBox(height: 10),
            _PreviewRow(label: 'ID', value: result.id),
            const SizedBox(height: 6),
            _PreviewRow(label: 'Categoria (código)', value: result.categoryCode),
            const SizedBox(height: 6),
            _PreviewRow(label: 'Código', value: result.code),
            const SizedBox(height: 6),
            _PreviewRow(label: 'Nome', value: result.name),
            const SizedBox(height: 6),
            _PreviewRow(label: 'Preço', value: _formatMoney(result.priceCents)),
            const SizedBox(height: 6),
            _PreviewRow(label: 'Ativo', value: result.isActive ? 'Sim' : 'Não'),
            if (result.averagePrepSeconds != null) ...[
              const SizedBox(height: 6),
              _PreviewRow(label: 'Preparo (s)', value: result.averagePrepSeconds.toString()),
            ],
            if (result.imageUrl != null && result.imageUrl!.trim().isNotEmpty) ...[
              const SizedBox(height: 6),
              _PreviewRow(label: 'Imagem', value: result.imageUrl!),
            ],
          ],
        ),
      ),
    );
  }
}

class _ProductsContent extends StatefulWidget {
  const _ProductsContent({
    required this.state,
  });

  final DashboardState state;

  @override
  State<_ProductsContent> createState() => _ProductsContentState();
}

class _ProductsContentState extends State<_ProductsContent> {
  late final TextEditingController _queryController;

  bool _includeInactive = true;
  bool _isLoading = false;
  bool _isLoadingMore = false;

  Map<String, String> _categoryNameByCode = const <String, String>{};
  List<CatalogAdminSkuResult> _items = const [];
  String? _nextCursorCode;
  String? _nextCursorId;

  bool get _hasMore => _nextCursorCode != null && _nextCursorId != null && _nextCursorId!.trim().isNotEmpty;

  @override
  void initState() {
    super.initState();
    _queryController = TextEditingController();
    Future<void>.microtask(() async {
      if (!mounted) return;
      await _loadCategories();
      await _search(reset: true);
    });
  }

  @override
  void dispose() {
    _queryController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _DateRangeHeader(from: widget.state.fromInclusive, to: widget.state.toInclusive),
        const SizedBox(height: 12),
        Card(
          clipBehavior: Clip.antiAlias,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Produtos', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w900)),
                const SizedBox(height: 12),
                TextField(
                  enabled: !_isLoading,
                  controller: _queryController,
                  decoration: const InputDecoration(
                    labelText: 'Buscar',
                    hintText: 'Código ou nome',
                    border: OutlineInputBorder(),
                  ),
                  onSubmitted: (_) => _search(reset: true),
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Switch(
                      value: _includeInactive,
                      onChanged: _isLoading ? null : (v) => setState(() => _includeInactive = v),
                    ),
                    const SizedBox(width: 8),
                    const Expanded(child: Text('Incluir inativos')),
                    FilledButton(
                      onPressed: _isLoading ? null : () => _search(reset: true),
                      child: _isLoading
                          ? const SizedBox(
                              height: 18,
                              width: 18,
                              child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                            )
                          : const Text('Buscar'),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),
        if (_items.isEmpty && _isLoading)
          const Center(
            child: Padding(
              padding: EdgeInsets.all(24),
              child: CircularProgressIndicator(),
            ),
          )
        else if (_items.isEmpty)
          const Padding(
            padding: EdgeInsets.symmetric(vertical: 8),
            child: Text('Nenhum produto encontrado.'),
          )
        else
          Card(
            clipBehavior: Clip.antiAlias,
            child: Column(
              children: [
                ListView.separated(
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  itemCount: _items.length,
                  separatorBuilder: (_, __) => const Divider(height: 1),
                  itemBuilder: (context, index) {
                    final sku = _items[index];
                    final categoryCode = sku.categoryCode;
                    final categoryName = categoryCode.isEmpty ? null : _categoryNameByCode[categoryCode];
                    final categoryLabel = categoryCode.isEmpty
                        ? null
                        : (categoryName == null || categoryName.trim().isEmpty ? categoryCode : categoryName);
                    return ListTile(
                      leading: categoryCode.isEmpty
                          ? null
                          : Chip(
                              label: Text(categoryLabel ?? categoryCode),
                            ),
                      title: Text(sku.name.isEmpty ? sku.code : sku.name),
                      subtitle: Text(
                        'Categoria: ${categoryLabel ?? '-'} • ${sku.code} • ${_formatMoney(sku.priceCents)}',
                      ),
                      trailing: sku.isActive ? const Icon(Icons.check_circle_outline) : const Icon(Icons.do_not_disturb_on_outlined),
                      onTap: () => _openEditSku(context, sku),
                    );
                  },
                ),
                if (_hasMore) ...[
                  const Divider(height: 1),
                  Padding(
                    padding: const EdgeInsets.all(12),
                    child: FilledButton(
                      onPressed: _isLoadingMore ? null : _loadMore,
                      child: _isLoadingMore
                          ? const SizedBox(
                              height: 18,
                              width: 18,
                              child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                            )
                          : const Text('Carregar mais'),
                    ),
                  ),
                ],
              ],
            ),
          ),
      ],
    );
  }

  Future<void> _search({required bool reset}) async {
    final repo = context.read<CatalogAdminRepository>();
    if (_isLoading) return;

    if (reset) {
      setState(() {
        _isLoading = true;
        _items = const [];
        _nextCursorCode = null;
        _nextCursorId = null;
      });
    } else {
      setState(() => _isLoading = true);
    }

    try {
      final page = await repo.searchSkus(
        query: _queryController.text.trim(),
        limit: 50,
        cursorCode: null,
        cursorId: null,
        includeInactive: _includeInactive,
      );
      if (!mounted) return;
      setState(() {
        _isLoading = false;
        _items = page.items;
        _nextCursorCode = page.nextCursorCode;
        _nextCursorId = page.nextCursorId;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => _isLoading = false);
      _showSnack(context, e.toString());
    }
  }

  Future<void> _loadCategories() async {
    final repo = context.read<CatalogAdminRepository>();
    try {
      final list = await repo.listCategories(includeInactive: true);
      if (!mounted) return;
      setState(() {
        _categoryNameByCode = <String, String>{
          for (final c in list) c.code: c.name,
        };
      });
    } catch (_) {}
  }

  Future<void> _loadMore() async {
    final repo = context.read<CatalogAdminRepository>();
    if (_isLoadingMore || !_hasMore) return;

    setState(() => _isLoadingMore = true);
    try {
      final page = await repo.searchSkus(
        query: _queryController.text.trim(),
        limit: 50,
        cursorCode: _nextCursorCode,
        cursorId: _nextCursorId,
        includeInactive: _includeInactive,
      );
      if (!mounted) return;
      setState(() {
        _isLoadingMore = false;
        _items = [..._items, ...page.items];
        _nextCursorCode = page.nextCursorCode;
        _nextCursorId = page.nextCursorId;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => _isLoadingMore = false);
      _showSnack(context, e.toString());
    }
  }

  Future<void> _openEditSku(BuildContext context, CatalogAdminSkuResult sku) async {
    final updated = await showDialog<CatalogAdminSkuResult?>(
      context: context,
      builder: (context) => _EditSkuDialog(sku: sku),
    );
    if (!mounted) return;
    if (updated == null) return;
    setState(() {
      _items = _items.map((x) => x.id == updated.id ? updated : x).toList(growable: false);
    });
  }
}

class _EditSkuDialog extends StatefulWidget {
  const _EditSkuDialog({
    required this.sku,
  });

  final CatalogAdminSkuResult sku;

  @override
  State<_EditSkuDialog> createState() => _EditSkuDialogState();
}

class _EditSkuDialogState extends State<_EditSkuDialog> {
  late final TextEditingController _categoryCodeController;
  late final TextEditingController _nameController;
  late final TextEditingController _priceController;
  late final TextEditingController _prepSecondsController;
  late final TextEditingController _imageUrlController;

  bool _isActive = true;
  bool _isSaving = false;

  @override
  void initState() {
    super.initState();
    _categoryCodeController = TextEditingController(text: widget.sku.categoryCode);
    _nameController = TextEditingController(text: widget.sku.name);
    _priceController = TextEditingController(text: (widget.sku.priceCents / 100).toStringAsFixed(2).replaceAll('.', ','));
    _prepSecondsController = TextEditingController(text: widget.sku.averagePrepSeconds?.toString() ?? '');
    _imageUrlController = TextEditingController(text: widget.sku.imageUrl ?? '');
    _isActive = widget.sku.isActive;
  }

  @override
  void dispose() {
    _categoryCodeController.dispose();
    _nameController.dispose();
    _priceController.dispose();
    _prepSecondsController.dispose();
    _imageUrlController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Editar produto'),
      content: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 560),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            _PreviewRow(label: 'Código Atual', value: widget.sku.code),
            const SizedBox(height: 12),
            TextField(
              enabled: !_isSaving,
              controller: _categoryCodeController,
              decoration: const InputDecoration(
                labelText: 'Categoria (código)',
                hintText: 'ex.: 00001',
                border: OutlineInputBorder(),
              ),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            TextField(
              enabled: !_isSaving,
              controller: _nameController,
              decoration: const InputDecoration(
                labelText: 'Nome',
                border: OutlineInputBorder(),
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              enabled: !_isSaving,
              controller: _priceController,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(
                labelText: 'Preço (R\$)',
                border: OutlineInputBorder(),
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              enabled: !_isSaving,
              controller: _prepSecondsController,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(
                labelText: 'Preparo (s) (opcional)',
                border: OutlineInputBorder(),
              ),
            ),
            const SizedBox(height: 12),
            TextField(
              enabled: !_isSaving,
              controller: _imageUrlController,
              decoration: const InputDecoration(
                labelText: 'Imagem (URL) (opcional)',
                border: OutlineInputBorder(),
              ),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Switch(value: _isActive, onChanged: _isSaving ? null : (v) => setState(() => _isActive = v)),
                const SizedBox(width: 8),
                const Text('Ativo'),
              ],
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: _isSaving ? null : () => Navigator.of(context).pop(null),
          child: const Text('Cancelar'),
        ),
        FilledButton(
          onPressed: _isSaving ? null : () => _save(context),
          child: _isSaving
              ? const SizedBox(
                  height: 18,
                  width: 18,
                  child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                )
              : const Text('Salvar'),
        ),
      ],
    );
  }

  Future<void> _save(BuildContext context) async {
    final repo = context.read<CatalogAdminRepository>();
    final categoryCode = _categoryCodeController.text.trim();
    final name = _nameController.text.trim();
    final priceCents = _parseMoneyToCents(_priceController.text);
    final prepSeconds = int.tryParse(_prepSecondsController.text.trim());
    final imageUrl = _imageUrlController.text.trim().isEmpty ? null : _imageUrlController.text.trim();

    if (categoryCode.isEmpty) {
      _showSnack(context, 'Código da Categoria inválido.');
      return;
    }
    if (name.length < 2) {
      _showSnack(context, 'Nome inválido.');
      return;
    }
    if (priceCents == null) {
      _showSnack(context, 'Preço inválido. Ex: 12,90');
      return;
    }
    if (_prepSecondsController.text.trim().isNotEmpty && (prepSeconds == null || prepSeconds <= 0)) {
      _showSnack(context, 'Tempo de preparo inválido.');
      return;
    }

    setState(() => _isSaving = true);
    try {
      final updated = await repo.updateSku(
        id: widget.sku.id,
        categoryCode: categoryCode,
        name: name,
        priceCents: priceCents,
        averagePrepSeconds: prepSeconds,
        imageUrl: imageUrl,
        isActive: _isActive,
      );
      if (!context.mounted) return;
      Navigator.of(context).pop(updated);
    } catch (e) {
      if (!context.mounted) return;
      setState(() => _isSaving = false);
      _showSnack(context, e.toString());
    }
  }
}

class _CreateSkuForm extends StatefulWidget {
  const _CreateSkuForm({
    required this.title,
    required this.subtitle,
    required this.state,
  });

  final String title;
  final String subtitle;
  final DashboardState state;

  @override
  State<_CreateSkuForm> createState() => _CreateSkuFormState();
}

class _CreateSkuFormState extends State<_CreateSkuForm> {
  late final TextEditingController _categoryCodeController;
  late final TextEditingController _nameController;
  late final TextEditingController _priceController;
  late final TextEditingController _prepSecondsController;
  late final TextEditingController _imageUrlController;

  bool _isActive = true;
  bool _isSubmitting = false;

  @override
  void initState() {
    super.initState();
    _categoryCodeController = TextEditingController();
    _nameController = TextEditingController();
    _priceController = TextEditingController();
    _prepSecondsController = TextEditingController();
    _imageUrlController = TextEditingController();
  }

  @override
  void dispose() {
    _categoryCodeController.dispose();
    _nameController.dispose();
    _priceController.dispose();
    _prepSecondsController.dispose();
    _imageUrlController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _DateRangeHeader(from: widget.state.fromInclusive, to: widget.state.toInclusive),
        const SizedBox(height: 12),
        Card(
          clipBehavior: Clip.antiAlias,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(widget.title, style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w900)),
                const SizedBox(height: 8),
                Text(widget.subtitle),
                const SizedBox(height: 16),
                TextField(
                  enabled: !_isSubmitting,
                  controller: _categoryCodeController,
                  decoration: const InputDecoration(
                    labelText: 'Categoria (código)',
                    hintText: 'ex.: 00001',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  enabled: !_isSubmitting,
                  controller: _nameController,
                  decoration: const InputDecoration(
                    labelText: 'Nome',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  enabled: !_isSubmitting,
                  controller: _priceController,
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  decoration: const InputDecoration(
                    labelText: 'Preço (R\$)',
                    hintText: 'ex.: 12,90',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  enabled: !_isSubmitting,
                  controller: _prepSecondsController,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(
                    labelText: 'Tempo médio de preparo (segundos) (opcional)',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  enabled: !_isSubmitting,
                  controller: _imageUrlController,
                  decoration: const InputDecoration(
                    labelText: 'Imagem (URL) (opcional)',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Switch(
                      value: _isActive,
                      onChanged: _isSubmitting ? null : (v) => setState(() => _isActive = v),
                    ),
                    const SizedBox(width: 8),
                    const Text('Ativo'),
                  ],
                ),
                const SizedBox(height: 16),
                FilledButton(
                  onPressed: _isSubmitting ? null : () => _submit(context),
                  child: _isSubmitting
                      ? const SizedBox(
                          height: 18,
                          width: 18,
                          child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                        )
                      : const Text('Salvar'),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Future<void> _submit(BuildContext context) async {
    final repo = context.read<CatalogAdminRepository>();

    final categoryCode = _categoryCodeController.text.trim();
    final name = _nameController.text.trim();
    final priceCents = _parseMoneyToCents(_priceController.text);
    final prepSeconds = int.tryParse(_prepSecondsController.text.trim());
    final imageUrl = _imageUrlController.text.trim().isEmpty ? null : _imageUrlController.text.trim();

    if (categoryCode.isEmpty) {
      _showSnack(context, 'Informe o código da Categoria.');
      return;
    }
    if (name.isEmpty) {
      _showSnack(context, 'Informe o nome.');
      return;
    }
    if (priceCents == null) {
      _showSnack(context, 'Preço inválido. Ex: 12,90');
      return;
    }
    if (_prepSecondsController.text.trim().isNotEmpty && (prepSeconds == null || prepSeconds <= 0)) {
      _showSnack(context, 'Tempo de preparo inválido.');
      return;
    }

    setState(() => _isSubmitting = true);
    try {
      final created = await repo.createSku(
        categoryCode: categoryCode,
        name: name,
        priceCents: priceCents,
        averagePrepSeconds: prepSeconds,
        imageUrl: imageUrl,
        isActive: _isActive,
      );
      if (!context.mounted) return;
      _showSnack(context, 'SKU criado: ${created.code}');
      _categoryCodeController.clear();
      _nameController.clear();
      _priceController.clear();
      _prepSecondsController.clear();
      _imageUrlController.clear();
      setState(() {});
    } catch (e) {
      if (!context.mounted) return;
      _showSnack(context, e.toString());
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }
}

class _PreviewRow extends StatelessWidget {
  const _PreviewRow({
    required this.label,
    required this.value,
  });

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Text(label, style: Theme.of(context).textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w800)),
        const SizedBox(width: 10),
        Expanded(child: Text(value, overflow: TextOverflow.ellipsis)),
      ],
    );
  }
}

void _showSnack(BuildContext context, String message) {
  ScaffoldMessenger.of(context).clearSnackBars();
  ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message)));
}

String _normalizeCategoryId(String raw) {
  final s = raw.trim().toLowerCase();
  return s.replaceAll(RegExp(r'[^a-z0-9]+'), '-').replaceAll(RegExp(r'-+'), '-').replaceAll(RegExp(r'^-+|-+$'), '');
}

int? _parseMoneyToCents(String raw) {
  var s = raw.trim();
  if (s.isEmpty) return null;
  s = s.replaceAll('R\$', '').trim();

  final hasComma = s.contains(',');
  final hasDot = s.contains('.');
  if (hasComma && hasDot) {
    s = s.replaceAll('.', '');
    s = s.replaceAll(',', '.');
  } else {
    s = s.replaceAll(',', '.');
  }

  final v = double.tryParse(s);
  if (v == null || v.isNaN || v.isInfinite) return null;
  final cents = (v * 100).round();
  if (cents < 0) return null;
  return cents;
}

Future<void> _pickDateRange(BuildContext context, DashboardState state, DashboardCubit cubit) async {
  final initialRange = DateTimeRange(start: state.fromInclusive, end: state.toInclusive);
  final range = await showDateRangePicker(
    context: context,
    firstDate: DateTime(2024, 1, 1),
    lastDate: DateTime.now().add(const Duration(days: 365)),
    initialDateRange: initialRange,
  );
  if (!context.mounted) return;
  if (range == null) return;

  final from = DateTime(range.start.year, range.start.month, range.start.day);
  final to = DateTime(range.end.year, range.end.month, range.end.day, 23, 59, 59);
  await cubit.setDateRange(fromInclusive: from, toInclusive: to);
}

class _DateRangeHeader extends StatelessWidget {
  const _DateRangeHeader({
    required this.from,
    required this.to,
  });

  final DateTime from;
  final DateTime to;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Text(
          'Período',
          style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800),
        ),
        const Spacer(),
        Text('${_formatDate(from)} → ${_formatDate(to)}'),
      ],
    );
  }
}

class _KpiGrid extends StatelessWidget {
  const _KpiGrid({required this.overview});

  final DashboardOverview overview;

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final cols = constraints.maxWidth >= 900 ? 4 : 2;
        final items = <_KpiItem>[
          _KpiItem(label: 'Faturamento', value: _formatMoney(overview.revenueCents), icon: Icons.payments_outlined),
          _KpiItem(label: 'Ticket médio', value: _formatMoney(overview.averageTicketCents), icon: Icons.shopping_cart_outlined),
          _KpiItem(label: 'Pedidos', value: overview.ordersCount.toString(), icon: Icons.receipt_long_outlined),
          _KpiItem(label: 'Pagos', value: overview.paidOrdersCount.toString(), icon: Icons.verified_outlined),
          _KpiItem(label: 'Cancelados', value: overview.cancelledOrdersCount.toString(), icon: Icons.block_outlined),
        ];

        return GridView.count(
          crossAxisCount: cols,
          physics: const NeverScrollableScrollPhysics(),
          shrinkWrap: true,
          childAspectRatio: constraints.maxWidth >= 900 ? 2.8 : 2.4,
          crossAxisSpacing: 12,
          mainAxisSpacing: 12,
          children: items.map((i) => _KpiCard(item: i)).toList(growable: false),
        );
      },
    );
  }
}

class _KpiItem {
  const _KpiItem({
    required this.label,
    required this.value,
    required this.icon,
  });

  final String label;
  final String value;
  final IconData icon;
}

class _KpiCard extends StatelessWidget {
  const _KpiCard({required this.item});

  final _KpiItem item;

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            Icon(item.icon),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(item.label, style: Theme.of(context).textTheme.bodyMedium),
                  const SizedBox(height: 6),
                  Text(item.value, style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w900)),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _BreakdownCard extends StatelessWidget {
  const _BreakdownCard({
    required this.title,
    required this.child,
    this.trailing,
  });

  final String title;
  final Widget child;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            Row(
              children: [
                Text(title, style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800)),
                const Spacer(),
                if (trailing != null) trailing!,
              ],
            ),
            const SizedBox(height: 12),
            child,
          ],
        ),
      ),
    );
  }
}

class _PaymentsByMethodList extends StatelessWidget {
  const _PaymentsByMethodList({required this.items});

  final List<DashboardPaymentMethodSummaryItem> items;

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) return const Text('Sem pagamentos aprovados no período.');
    final sorted = items.toList(growable: false)
      ..sort((a, b) => b.amountCents.compareTo(a.amountCents));

    return Column(
      children: sorted
          .map(
            (i) => Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: Row(
                children: [
                  Expanded(child: Text(_paymentMethodLabel(i.method))),
                  Text('${_formatMoney(i.amountCents)} (${i.paymentsCount})'),
                ],
              ),
            ),
          )
          .toList(growable: false),
    );
  }
}

class _PaymentsByProviderList extends StatelessWidget {
  const _PaymentsByProviderList({required this.items});

  final List<DashboardPaymentProviderSummaryItem> items;

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) return const Text('Sem faturamento no período.');
    final sorted = items.toList(growable: false)..sort((a, b) => b.amountCents.compareTo(a.amountCents));

    return Column(
      children: sorted
          .map(
            (i) => Padding(
              padding: const EdgeInsets.only(bottom: 10),
              child: Row(
                children: [
                  Expanded(child: Text(_providerLabel(i.provider))),
                  Text('${_formatMoney(i.amountCents)} (${i.paymentsCount})'),
                ],
              ),
            ),
          )
          .toList(growable: false),
    );
  }
}

class _KitchenStatusList extends StatelessWidget {
  const _KitchenStatusList({required this.items});

  final List<DashboardKitchenStatusSummaryItem> items;

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) return const Text('Sem dados no período.');
    final sorted = items.toList(growable: false)..sort((a, b) => b.ordersCount.compareTo(a.ordersCount));

    return Wrap(
      spacing: 10,
      runSpacing: 10,
      children: sorted
          .map(
            (i) => Chip(
              label: Text('${_kitchenStatusLabel(i.kitchenStatus)}: ${i.ordersCount}'),
            ),
          )
          .toList(growable: false),
    );
  }
}

class _RecentOrdersList extends StatelessWidget {
  const _RecentOrdersList({required this.orders});

  final List<DashboardOrder> orders;

  @override
  Widget build(BuildContext context) {
    return ListView.separated(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      itemCount: orders.length,
      separatorBuilder: (_, __) => const Divider(height: 18),
      itemBuilder: (context, index) {
        final o = orders[index];
        return Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Pedido ${o.orderId.substring(0, 8)}${o.comanda == null || o.comanda!.trim().isEmpty ? '' : ' • Comanda ${o.comanda}'}',
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w900),
                  ),
                  const SizedBox(height: 6),
                  Text('${_statusLabel(o.status)} • ${_kitchenStatusLabel(o.kitchenStatus)}'),
                  const SizedBox(height: 6),
                  Text('Atualizado: ${_formatDateTime(o.updatedAt)}'),
                  if (o.paymentStatus != null) ...[
                    const SizedBox(height: 6),
                    Text(
                      'Pagamento: ${o.paymentStatus}${o.paymentMethod != null ? ' • ${_paymentMethodLabel(o.paymentMethod!)}' : ''}',
                    ),
                  ],
                ],
              ),
            ),
            const SizedBox(width: 12),
            Text(
              _formatMoney(o.totalCents),
              style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w900),
            ),
          ],
        );
      },
    );
  }
}

String _formatMoney(int cents) {
  final value = cents / 100;
  return 'R\$ ${value.toStringAsFixed(2).replaceAll('.', ',')}';
}

String _formatDate(DateTime dt) {
  String two(int v) => v.toString().padLeft(2, '0');
  return '${two(dt.day)}/${two(dt.month)}/${dt.year}';
}

String _formatDateTime(DateTime dt) {
  String two(int v) => v.toString().padLeft(2, '0');
  return '${two(dt.day)}/${two(dt.month)} ${two(dt.hour)}:${two(dt.minute)}';
}

String _paymentMethodLabel(PaymentMethod v) {
  return switch (v) {
    PaymentMethod.creditCard => 'Crédito',
    PaymentMethod.debitCard => 'Débito',
    PaymentMethod.pix => 'Pix',
    PaymentMethod.cash => 'Dinheiro',
  };
}

String _providerLabel(String v) {
  final normalized = v.trim().toUpperCase();
  return switch (normalized) {
    'POS' => 'PDV',
    'TEF' => 'TEF',
    _ => v.trim().isEmpty ? 'Outro' : v.trim(),
  };
}

String _statusLabel(String v) {
  return switch (v) {
    'Created' => 'Criado',
    'Paid' => 'Pago',
    'Cancelled' => 'Cancelado',
    _ => v,
  };
}

String _kitchenStatusLabel(String v) {
  return switch (v) {
    'PendingPayment' => 'Aguardando pagamento',
    'Queued' => 'Fila',
    'InPreparation' => 'Em preparo',
    'Ready' => 'Pronto',
    'Completed' => 'Finalizado',
    'Cancelled' => 'Cancelado',
    _ => v,
  };
}
