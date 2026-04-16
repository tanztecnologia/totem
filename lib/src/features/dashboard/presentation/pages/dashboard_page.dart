import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../checkout/domain/entities/checkout_order.dart';
import '../../../identity/presentation/bloc/auth_cubit.dart';
import '../../domain/entities/dashboard_order.dart';
import '../../domain/entities/dashboard_overview.dart';
import '../../domain/repositories/catalog_admin_repository.dart';
import '../bloc/dashboard_cubit.dart';
import '../bloc/dashboard_state.dart';

part 'dashboard_page.catalog.dart';
part 'dashboard_page.shared.dart';

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
