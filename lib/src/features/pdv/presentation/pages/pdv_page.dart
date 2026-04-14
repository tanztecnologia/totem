import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../checkout/domain/entities/checkout_order.dart';
import '../../../identity/domain/entities/auth_session.dart';
import '../../../identity/presentation/bloc/auth_cubit.dart';
import '../../domain/entities/pdv_order.dart';
import '../bloc/pdv_cubit.dart';
import '../bloc/pdv_state.dart';

class PdvPage extends StatefulWidget {
  const PdvPage({super.key});

  @override
  State<PdvPage> createState() => _PdvPageState();
}

class _PdvPageState extends State<PdvPage> {
  late final TextEditingController _comandaController;
  late final TextEditingController _transactionIdController;
  late final FocusNode _comandaFocus;

  @override
  void initState() {
    super.initState();
    _comandaController = TextEditingController();
    _transactionIdController = TextEditingController();
    _comandaFocus = FocusNode();
  }

  @override
  void dispose() {
    _comandaController.dispose();
    _transactionIdController.dispose();
    _comandaFocus.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return BlocConsumer<PdvCubit, PdvState>(
      listener: (context, state) {
        if (state.errorMessage != null && state.errorMessage!.trim().isNotEmpty) {
          ScaffoldMessenger.of(context).clearSnackBars();
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text(state.errorMessage!)),
          );
        }
        final payment = state.lastPayment;
        if (payment != null && payment.isApproved) {
          ScaffoldMessenger.of(context).clearSnackBars();
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('Pagamento aprovado (${payment.transactionId})')),
          );
        }
        final opened = state.lastCashRegisterOpenedShift;
        if (opened != null) {
          ScaffoldMessenger.of(context).clearSnackBars();
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('Caixa aberto (${_formatMoney(opened.openingCashCents)})')),
          );
          context.read<PdvCubit>().clearCashRegisterNotifications();
        }
        final closed = state.lastCashRegisterClosedResult;
        if (closed != null) {
          ScaffoldMessenger.of(context).clearSnackBars();
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text('Caixa fechado. Diferença: ${_formatMoney(closed.differenceCents)}')),
          );
          context.read<PdvCubit>().clearCashRegisterNotifications();
        }
      },
      builder: (context, state) {
        final cubit = context.read<PdvCubit>();
        final session = context.select<AuthCubit, AuthSession?>((c) => c.session);
        if (_comandaController.text != state.comanda) {
          _comandaController.value = _comandaController.value.copyWith(
            text: state.comanda,
            selection: TextSelection.collapsed(offset: state.comanda.length),
          );
        }
        if (_transactionIdController.text != state.transactionId) {
          _transactionIdController.value = _transactionIdController.value.copyWith(
            text: state.transactionId,
            selection: TextSelection.collapsed(offset: state.transactionId.length),
          );
        }

        final orders = state.orders.toList(growable: false)
          ..sort((a, b) => b.updatedAt.compareTo(a.updatedAt));
        final selected = _findOrderById(orders, state.selectedOrderId);
        final shift = state.cashRegisterShift;
        final isShiftOpen = shift?.isOpen == true;
        final cashStatusLabel = isShiftOpen ? 'Caixa aberto' : 'Caixa fechado';

        return Scaffold(
          appBar: AppBar(
            title: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('PDV'),
                if (session != null)
                  Text(
                    '${session.email} (${session.role})',
                    style: Theme.of(context).textTheme.bodySmall,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
              ],
            ),
            actions: [
              Center(
                child: Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 8),
                  child: Chip(
                    label: Text(cashStatusLabel),
                  ),
                ),
              ),
              IconButton(
                onPressed: state.isCashRegisterLoading ? null : () => cubit.loadCashRegister(),
                icon: state.isCashRegisterLoading
                    ? const SizedBox(
                        height: 18,
                        width: 18,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Icon(Icons.point_of_sale),
              ),
              IconButton(
                onPressed: state.isCashRegisterBusy
                    ? null
                    : () async {
                        if (isShiftOpen) {
                          final cents = await _askMoneyCents(
                            context,
                            title: 'Fechar caixa',
                            labelText: 'Dinheiro em caixa (R\$)',
                            confirmText: 'Fechar',
                          );
                          if (cents == null) return;
                          await cubit.closeCashRegister(closingCashCents: cents);
                          await cubit.loadCashRegister();
                          return;
                        }

                        final cents = await _askMoneyCents(
                          context,
                          title: 'Abrir caixa',
                          labelText: 'Troco inicial (R\$)',
                          confirmText: 'Abrir',
                        );
                        if (cents == null) return;
                        await cubit.openCashRegister(openingCashCents: cents);
                        await cubit.loadCashRegister();
                      },
                icon: Icon(isShiftOpen ? Icons.lock : Icons.lock_open),
              ),
              IconButton(
                onPressed: state.isLoading ? null : () => cubit.search(),
                icon: const Icon(Icons.refresh),
              ),
            ],
          ),
          body: SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: LayoutBuilder(
                builder: (context, constraints) {
                  final isWide = constraints.maxWidth >= 900;

                  final searchPanel = _SearchPanel(
                    comandaController: _comandaController,
                    comandaFocus: _comandaFocus,
                    isLoading: state.isLoading,
                    includePaid: state.includePaid,
                    ordersCount: orders.length,
                    onComandaChanged: cubit.setComanda,
                    onIncludePaidChanged: cubit.setIncludePaid,
                    onClear: () {
                      _comandaController.clear();
                      cubit.setComanda('');
                    },
                    onSearch: cubit.search,
                  );

                  final ordersPanel = _OrdersPanel(
                    isLoading: state.isLoading,
                    orders: orders,
                    selectedOrderId: state.selectedOrderId,
                    onSelectOrder: cubit.selectOrder,
                  );

                  final paymentPanel = _PaymentPanel(
                    isPaying: state.isPaying,
                    payingOrderId: state.payingOrderId,
                    order: selected,
                    paymentMethod: state.paymentMethod,
                    transactionIdController: _transactionIdController,
                    onPaymentMethodChanged: cubit.setPaymentMethod,
                    onTransactionIdChanged: cubit.setTransactionId,
                    onPay: selected == null ||
                            selected.isPaid ||
                            state.isPaying ||
                            state.paymentMethod == null ||
                            state.cashRegisterShift?.isOpen != true
                        ? null
                        : () => _confirmPayment(
                              context,
                              orderId: selected.orderId,
                              totalCents: selected.totalCents,
                              paymentMethod: state.paymentMethod!,
                              transactionId: state.transactionId,
                            ),
                  );

                  if (isWide) {
                    return Row(
                      children: [
                        Expanded(
                          flex: 3,
                          child: Column(
                            children: [
                              searchPanel,
                              const SizedBox(height: 12),
                              Expanded(child: ordersPanel),
                            ],
                          ),
                        ),
                        const SizedBox(width: 16),
                        SizedBox(width: 380, child: paymentPanel),
                      ],
                    );
                  }

                  return Column(
                    children: [
                      searchPanel,
                      const SizedBox(height: 12),
                      Expanded(child: ordersPanel),
                      const SizedBox(height: 12),
                      paymentPanel,
                    ],
                  );
                },
              ),
            ),
          ),
        );
      },
    );
  }

  Future<void> _confirmPayment(
    BuildContext context, {
    required String orderId,
    required int totalCents,
    required PaymentMethod paymentMethod,
    required String transactionId,
  }) async {
    final cubit = context.read<PdvCubit>();
    final ok = await showDialog<bool>(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: const Text('Confirmar recebimento'),
          content: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 520),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Pedido ${orderId.substring(0, 8)}'),
                const SizedBox(height: 10),
                Text(
                  _formatMoney(totalCents),
                  style: Theme.of(context).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w900),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 10),
                Text('Forma de pagamento: ${_paymentMethodLabel(paymentMethod)}'),
                if (transactionId.trim().isNotEmpty && paymentMethod != PaymentMethod.cash) ...[
                  const SizedBox(height: 10),
                  Text('Transação: ${transactionId.trim()}'),
                ],
              ],
            ),
          ),
          actions: [
            TextButton(onPressed: () => Navigator.of(context).pop(false), child: const Text('Cancelar')),
            FilledButton(onPressed: () => Navigator.of(context).pop(true), child: const Text('Confirmar')),
          ],
        );
      },
    );

    if (ok != true) return;

    await cubit.pay(
      orderId: orderId,
      method: paymentMethod,
      transactionId: transactionId.trim().isEmpty ? null : transactionId.trim(),
    );
  }
}

class _SearchPanel extends StatelessWidget {
  const _SearchPanel({
    required this.comandaController,
    required this.comandaFocus,
    required this.isLoading,
    required this.includePaid,
    required this.ordersCount,
    required this.onComandaChanged,
    required this.onIncludePaidChanged,
    required this.onClear,
    required this.onSearch,
  });

  final TextEditingController comandaController;
  final FocusNode comandaFocus;
  final bool isLoading;
  final bool includePaid;
  final int ordersCount;
  final ValueChanged<String> onComandaChanged;
  final ValueChanged<bool> onIncludePaidChanged;
  final VoidCallback onClear;
  final VoidCallback onSearch;

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          children: [
            Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: comandaController,
                    focusNode: comandaFocus,
                    enabled: !isLoading,
                    onChanged: onComandaChanged,
                    onSubmitted: (_) => onSearch(),
                    textInputAction: TextInputAction.search,
                    decoration: const InputDecoration(
                      labelText: 'Comanda',
                      hintText: 'Digite e pressione Enter',
                      border: OutlineInputBorder(),
                      prefixIcon: Icon(Icons.qr_code_scanner),
                    ),
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                ),
                const SizedBox(width: 12),
                FilledButton.icon(
                  onPressed: isLoading ? null : onSearch,
                  icon: isLoading
                      ? const SizedBox(
                          height: 18,
                          width: 18,
                          child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                        )
                      : const Icon(Icons.search),
                  label: const Text('Buscar'),
                ),
                const SizedBox(width: 10),
                OutlinedButton(
                  onPressed: isLoading ? null : () {
                    onClear();
                    FocusScope.of(context).requestFocus(comandaFocus);
                  },
                  child: const Text('Limpar'),
                ),
              ],
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Switch(
                  value: includePaid,
                  onChanged: isLoading ? null : onIncludePaidChanged,
                ),
                const SizedBox(width: 8),
                const Text('Incluir pagos'),
                const Spacer(),
                Text('$ordersCount pedido(s)'),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _OrdersPanel extends StatelessWidget {
  const _OrdersPanel({
    required this.isLoading,
    required this.orders,
    required this.selectedOrderId,
    required this.onSelectOrder,
  });

  final bool isLoading;
  final List<PdvOrder> orders;
  final String? selectedOrderId;
  final ValueChanged<String?> onSelectOrder;

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(14),
            child: Row(
              children: [
                Text('Pedidos', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
                const Spacer(),
                if (isLoading) const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2)),
              ],
            ),
          ),
          const Divider(height: 1),
          Expanded(
            child: orders.isEmpty
                ? const Center(child: Text('Nenhum pedido para esta comanda.'))
                : ListView.separated(
                    padding: const EdgeInsets.all(10),
                    itemCount: orders.length,
                    separatorBuilder: (_, __) => const SizedBox(height: 10),
                    itemBuilder: (context, index) {
                      final o = orders[index];
                      final orderId = o.orderId;
                      final isSelected = selectedOrderId == orderId;
                      final isPaid = o.isPaid;

                      return Material(
                        color: isSelected ? Theme.of(context).colorScheme.primaryContainer : Theme.of(context).cardColor,
                        borderRadius: BorderRadius.circular(12),
                        child: InkWell(
                          borderRadius: BorderRadius.circular(12),
                          onTap: () => onSelectOrder(orderId),
                          child: Padding(
                            padding: const EdgeInsets.all(14),
                            child: Row(
                              children: [
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Row(
                                        children: [
                                          Text(
                                            'Pedido ${orderId.substring(0, 8)}',
                                            style: Theme.of(context)
                                                .textTheme
                                                .titleMedium
                                                ?.copyWith(fontWeight: FontWeight.w900),
                                          ),
                                          const SizedBox(width: 10),
                                          if (isPaid) const Chip(label: Text('Pago')),
                                        ],
                                      ),
                                      const SizedBox(height: 6),
                                      Text('Status: ${o.status} / ${o.kitchenStatus}'),
                                      const SizedBox(height: 6),
                                      Text('Atualizado: ${_formatDateTime(o.updatedAt)}'),
                                    ],
                                  ),
                                ),
                                const SizedBox(width: 12),
                                Text(
                                  _formatMoney(o.totalCents),
                                  style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w900),
                                ),
                              ],
                            ),
                          ),
                        ),
                      );
                    },
                  ),
          ),
        ],
      ),
    );
  }
}

class _PaymentPanel extends StatelessWidget {
  const _PaymentPanel({
    required this.isPaying,
    required this.payingOrderId,
    required this.order,
    required this.paymentMethod,
    required this.transactionIdController,
    required this.onPaymentMethodChanged,
    required this.onTransactionIdChanged,
    required this.onPay,
  });

  final bool isPaying;
  final String? payingOrderId;
  final PdvOrder? order;
  final PaymentMethod? paymentMethod;
  final TextEditingController transactionIdController;
  final ValueChanged<PaymentMethod?> onPaymentMethodChanged;
  final ValueChanged<String> onTransactionIdChanged;
  final VoidCallback? onPay;

  @override
  Widget build(BuildContext context) {
    final o = order;
    return Card(
      clipBehavior: Clip.antiAlias,
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: o == null
            ? Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text('Pagamento', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
                  const SizedBox(height: 12),
                  const Text('Selecione um pedido para receber o pagamento.'),
                ],
              )
            : Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text('Pagamento', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
                  const SizedBox(height: 12),
                  Text('Pedido ${o.orderId.substring(0, 8)}'),
                  const SizedBox(height: 6),
                  Text('Status: ${o.status} / ${o.kitchenStatus}'),
                  const SizedBox(height: 14),
                  Text(
                    _formatMoney(o.totalCents),
                    style: Theme.of(context).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w900),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 14),
                  Text(
                    'Selecione a forma de pagamento:',
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
                  ),
                  const SizedBox(height: 10),
                  SegmentedButton<PaymentMethod>(
                    segments: const <ButtonSegment<PaymentMethod>>[
                      ButtonSegment(value: PaymentMethod.creditCard, label: Text('Crédito')),
                      ButtonSegment(value: PaymentMethod.debitCard, label: Text('Débito')),
                      ButtonSegment(value: PaymentMethod.pix, label: Text('Pix')),
                      ButtonSegment(value: PaymentMethod.cash, label: Text('Dinheiro')),
                    ],
                    selected: paymentMethod == null ? const <PaymentMethod>{} : <PaymentMethod>{paymentMethod!},
                    emptySelectionAllowed: true,
                    onSelectionChanged: isPaying
                        ? null
                        : (selection) {
                            onPaymentMethodChanged(selection.isEmpty ? null : selection.first);
                          },
                  ),
                  if (paymentMethod != null && paymentMethod != PaymentMethod.cash) ...[
                    const SizedBox(height: 12),
                    TextField(
                      enabled: !isPaying,
                      controller: transactionIdController,
                      onChanged: onTransactionIdChanged,
                      decoration: const InputDecoration(
                        labelText: 'TransactionId (opcional)',
                        border: OutlineInputBorder(),
                      ),
                    ),
                  ],
                  const SizedBox(height: 14),
                  FilledButton(
                    onPressed: onPay,
                    style: FilledButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 18),
                    ),
                    child: isPaying && payingOrderId == o.orderId
                        ? const SizedBox(
                            height: 20,
                            width: 20,
                            child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                          )
                        : Text(
                            o.isPaid
                                ? 'Pedido pago'
                                : (paymentMethod == null ? 'Selecione a forma de pagamento' : 'Receber'),
                          ),
                  ),
                ],
              ),
      ),
    );
  }
}

Future<int?> _askMoneyCents(
  BuildContext context, {
  required String title,
  required String labelText,
  required String confirmText,
}) async {
  final controller = TextEditingController();
  final ok = await showDialog<bool>(
    context: context,
    builder: (context) {
      return AlertDialog(
        title: Text(title),
        content: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 520),
          child: TextField(
            controller: controller,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: InputDecoration(
              labelText: labelText,
              border: const OutlineInputBorder(),
            ),
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.of(context).pop(false), child: const Text('Cancelar')),
          FilledButton(onPressed: () => Navigator.of(context).pop(true), child: Text(confirmText)),
        ],
      );
    },
  );

  if (!context.mounted) {
    controller.dispose();
    return null;
  }

  if (ok != true) {
    controller.dispose();
    return null;
  }

  final cents = _parseMoneyToCents(controller.text);
  controller.dispose();
  if (cents == null) {
    ScaffoldMessenger.of(context).clearSnackBars();
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Valor inválido. Ex: 100,00')),
    );
    return null;
  }
  return cents;
}

String _formatMoney(int cents) {
  final value = cents / 100;
  return 'R\$ ${value.toStringAsFixed(2).replaceAll('.', ',')}';
}

int? _parseMoneyToCents(String raw) {
  var s = raw.trim();
  if (s.isEmpty) return 0;
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

String _formatDateTime(DateTime dt) {
  String two(int v) => v.toString().padLeft(2, '0');
  return '${two(dt.day)}/${two(dt.month)} ${two(dt.hour)}:${two(dt.minute)}';
}

String _paymentMethodLabel(PaymentMethod v) {
  return switch (v) {
    PaymentMethod.creditCard => 'Cartão de crédito',
    PaymentMethod.debitCard => 'Cartão de débito',
    PaymentMethod.pix => 'Pix',
    PaymentMethod.cash => 'Dinheiro',
  };
}

PdvOrder? _findOrderById(List<PdvOrder> orders, String? orderId) {
  if (orderId == null || orderId.trim().isEmpty) return null;
  for (final o in orders) {
    if (o.orderId == orderId) return o;
  }
  return null;
}
