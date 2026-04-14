import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../checkout/domain/entities/checkout_order.dart';
import '../../../identity/presentation/bloc/auth_cubit.dart';
import '../../domain/entities/dashboard_order.dart';
import '../../domain/entities/dashboard_overview.dart';
import '../bloc/dashboard_cubit.dart';
import '../bloc/dashboard_state.dart';

class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key});

  @override
  State<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends State<DashboardPage> {
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
            child: RefreshIndicator(
              onRefresh: cubit.refreshAll,
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  _DateRangeHeader(from: state.fromInclusive, to: state.toInclusive),
                  const SizedBox(height: 12),
                  if (overview == null)
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
                    _KpiGrid(overview: overview),
                    const SizedBox(height: 12),
                    _BreakdownCard(
                      title: 'Pagamentos por método',
                      child: _PaymentsByMethodList(items: overview.paymentsByMethod),
                    ),
                    const SizedBox(height: 12),
                    _BreakdownCard(
                      title: 'Faturamento por tipo',
                      child: _PaymentsByProviderList(items: overview.paymentsByProvider),
                    ),
                    const SizedBox(height: 12),
                    _BreakdownCard(
                      title: 'Pedidos por status (cozinha)',
                      child: _KitchenStatusList(items: overview.ordersByKitchenStatus),
                    ),
                  ],
                  const SizedBox(height: 12),
                  _BreakdownCard(
                    title: 'Pedidos recentes',
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
              ),
            ),
          ),
        );
      },
    );
  }
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
