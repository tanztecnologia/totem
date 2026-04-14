import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../domain/entities/checkout_item.dart';
import '../../domain/entities/checkout_order.dart';
import '../../domain/services/checkout_service.dart';
import '../../domain/usecases/confirm_payment.dart';
import '../../domain/usecases/start_checkout.dart';
import '../bloc/checkout_bloc.dart';

class CheckoutDialog extends StatelessWidget {
  const CheckoutDialog({
    required this.items,
    required this.totalCents,
    required this.totalText,
    required this.checkoutService,
    required this.onSuccess,
    super.key,
  });

  final List<CheckoutItem> items;
  final int totalCents;
  final String totalText;
  final CheckoutService checkoutService;
  final VoidCallback onSuccess;

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => CheckoutBloc(
        startCheckout: StartCheckout(checkoutService),
        confirmPayment: ConfirmPayment(checkoutService),
      )..add(
          CheckoutStarted(
            items: items,
            totalCents: totalCents,
            totalText: totalText,
          ),
        ),
      child: Dialog(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 560),
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: BlocBuilder<CheckoutBloc, CheckoutState>(
              builder: (context, state) {
                void closeAndNotify() {
                  Navigator.of(context).pop();
                  onSuccess();
                }

                final title = switch (state.step) {
                  CheckoutStep.fulfillment => 'Consumo',
                  CheckoutStep.payment => 'Pagamento',
                  CheckoutStep.pixQr => 'Pix',
                  CheckoutStep.cardPrompt => 'Cartão',
                  CheckoutStep.success => 'Pedido confirmado',
                };

                return Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Text(
                      title,
                      style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.w900),
                    ),
                    const SizedBox(height: 14),
                    ConstrainedBox(
                      constraints: const BoxConstraints(maxHeight: 520),
                      child: SingleChildScrollView(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            if (state.step == CheckoutStep.fulfillment) ...[
                              Text(
                                'Seu pedido é para comer no local ou para levar?',
                                style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
                              ),
                              const SizedBox(height: 14),
                              FilledButton(
                                onPressed: () => context.read<CheckoutBloc>().add(
                                      const CheckoutFulfillmentSelected(OrderFulfillment.dineIn),
                                    ),
                                child: const Padding(
                                  padding: EdgeInsets.symmetric(vertical: 14),
                                  child: Text('Comer no local'),
                                ),
                              ),
                              const SizedBox(height: 10),
                              OutlinedButton(
                                onPressed: () => context.read<CheckoutBloc>().add(
                                      const CheckoutFulfillmentSelected(OrderFulfillment.takeAway),
                                    ),
                                child: const Padding(
                                  padding: EdgeInsets.symmetric(vertical: 14),
                                  child: Text('Para levar'),
                                ),
                              ),
                            ],
                            if (state.step == CheckoutStep.payment) ...[
                              Text(
                                'Selecione a forma de pagamento:',
                                style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
                              ),
                              const SizedBox(height: 10),
                              SegmentedButton<PaymentMethod>(
                                segments: const <ButtonSegment<PaymentMethod>>[
                                  ButtonSegment(
                                    value: PaymentMethod.creditCard,
                                    label: Text('Crédito'),
                                  ),
                                  ButtonSegment(
                                    value: PaymentMethod.debitCard,
                                    label: Text('Débito'),
                                  ),
                                  ButtonSegment(
                                    value: PaymentMethod.pix,
                                    label: Text('Pix'),
                                  ),
                                ],
                                selected: state.paymentMethod == null
                                    ? const <PaymentMethod>{}
                                    : <PaymentMethod>{state.paymentMethod!},
                                emptySelectionAllowed: true,
                                onSelectionChanged: (selection) {
                                  if (selection.isEmpty) return;
                                  context.read<CheckoutBloc>().add(CheckoutPaymentMethodSelected(selection.first));
                                },
                              ),
                              if (state.errorMessage != null) ...[
                                const SizedBox(height: 10),
                                Text(
                                  state.errorMessage!,
                                  style: TextStyle(
                                    color: Theme.of(context).colorScheme.error,
                                    fontWeight: FontWeight.w800,
                                  ),
                                ),
                              ],
                              const SizedBox(height: 12),
                              DecoratedBox(
                                decoration: BoxDecoration(
                                  color: Theme.of(context).colorScheme.surfaceContainerHighest,
                                  borderRadius: BorderRadius.circular(14),
                                ),
                                child: Padding(
                                  padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
                                  child: Row(
                                    children: [
                                      const Text('Total', style: TextStyle(fontWeight: FontWeight.w800)),
                                      const Spacer(),
                                      Text(
                                        totalText,
                                        style: TextStyle(
                                          fontWeight: FontWeight.w900,
                                          color: Theme.of(context).colorScheme.primary,
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                              const SizedBox(height: 14),
                              Row(
                                children: [
                                  Expanded(
                                    child: OutlinedButton(
                                      onPressed: state.isSubmitting
                                          ? null
                                          : () => context.read<CheckoutBloc>().add(const CheckoutBackRequested()),
                                      child: const Padding(
                                        padding: EdgeInsets.symmetric(vertical: 12),
                                        child: Text('Voltar'),
                                      ),
                                    ),
                                  ),
                                  const SizedBox(width: 10),
                                  Expanded(
                                    child: FilledButton(
                                      onPressed: state.isSubmitting || state.paymentMethod == null
                                          ? null
                                          : () => context.read<CheckoutBloc>().add(const CheckoutConfirmed()),
                                      child: Padding(
                                        padding: const EdgeInsets.symmetric(vertical: 12),
                                        child: state.isSubmitting
                                            ? const SizedBox(
                                                height: 18,
                                                width: 18,
                                                child: CircularProgressIndicator(strokeWidth: 2),
                                              )
                                            : Text(state.paymentMethod == PaymentMethod.pix ? 'Gerar QR' : 'Finalizar'),
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                            ],
                            if (state.step == CheckoutStep.pixQr) ...[
                              Text(
                                'Escaneie o QR Code para pagar via Pix:',
                                style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
                              ),
                              const SizedBox(height: 12),
                              Center(
                                child: _QrLikeBox(
                                  data: state.pixCharge?.payload ?? '',
                                  size: 260,
                                ),
                              ),
                              const SizedBox(height: 12),
                              DecoratedBox(
                                decoration: BoxDecoration(
                                  color: Theme.of(context).colorScheme.surfaceContainerHighest,
                                  borderRadius: BorderRadius.circular(14),
                                ),
                                child: Padding(
                                  padding: const EdgeInsets.all(14),
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.stretch,
                                    children: [
                                      Row(
                                        children: [
                                          const Text('Valor', style: TextStyle(fontWeight: FontWeight.w900)),
                                          const Spacer(),
                                          Text(
                                            totalText,
                                            style: TextStyle(
                                              fontWeight: FontWeight.w900,
                                              color: Theme.of(context).colorScheme.primary,
                                            ),
                                          ),
                                        ],
                                      ),
                                      const SizedBox(height: 8),
                                      Row(
                                        children: [
                                          const Text('Válido até', style: TextStyle(fontWeight: FontWeight.w800)),
                                          const Spacer(),
                                          Text(_formatDateTime(state.pixCharge?.expiresAt)),
                                        ],
                                      ),
                                      const SizedBox(height: 12),
                                      Text(
                                        'Código Pix (copia e cola)',
                                        style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w900),
                                      ),
                                      const SizedBox(height: 6),
                                      SelectableText(
                                        state.pixCharge?.payload ?? '',
                                        style: TextStyle(color: Theme.of(context).colorScheme.onSurfaceVariant),
                                      ),
                                      const SizedBox(height: 10),
                                      OutlinedButton(
                                        onPressed: (state.pixCharge?.payload.isEmpty ?? true)
                                            ? null
                                            : () async {
                                                await Clipboard.setData(ClipboardData(text: state.pixCharge!.payload));
                                              },
                                        child: const Padding(
                                          padding: EdgeInsets.symmetric(vertical: 10),
                                          child: Text('Copiar código'),
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                              if (state.errorMessage != null) ...[
                                const SizedBox(height: 10),
                                Text(
                                  state.errorMessage!,
                                  style: TextStyle(
                                    color: Theme.of(context).colorScheme.error,
                                    fontWeight: FontWeight.w800,
                                  ),
                                ),
                              ],
                              const SizedBox(height: 14),
                              Row(
                                children: [
                                  Expanded(
                                    child: OutlinedButton(
                                      onPressed: state.isSubmitting
                                          ? null
                                          : () => context.read<CheckoutBloc>().add(const CheckoutBackRequested()),
                                      child: const Padding(
                                        padding: EdgeInsets.symmetric(vertical: 12),
                                        child: Text('Voltar'),
                                      ),
                                    ),
                                  ),
                                  const SizedBox(width: 10),
                                  Expanded(
                                    child: FilledButton(
                                      onPressed: state.isSubmitting
                                          ? null
                                          : () => context.read<CheckoutBloc>().add(const CheckoutPixPaymentConfirmed()),
                                      child: Padding(
                                        padding: const EdgeInsets.symmetric(vertical: 12),
                                        child: state.isSubmitting
                                            ? const SizedBox(
                                                height: 18,
                                                width: 18,
                                                child: CircularProgressIndicator(strokeWidth: 2),
                                              )
                                            : const Text('Confirmar pagamento'),
                                      ),
                                    ),
                                  ),
                                ],
                              ),
                            ],
                            if (state.step == CheckoutStep.cardPrompt) ...[
                              Text(
                                'Aproxime ou insira o cartão para pagar:',
                                style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700),
                              ),
                              const SizedBox(height: 12),
                              DecoratedBox(
                                decoration: BoxDecoration(
                                  color: Theme.of(context).colorScheme.surfaceContainerHighest,
                                  borderRadius: BorderRadius.circular(14),
                                ),
                                child: Padding(
                                  padding: const EdgeInsets.all(14),
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.stretch,
                                    children: [
                                      Row(
                                        children: [
                                          const Text('Forma', style: TextStyle(fontWeight: FontWeight.w800)),
                                          const Spacer(),
                                          Text(_paymentMethodLabel(state.paymentMethod)),
                                        ],
                                      ),
                                      const SizedBox(height: 8),
                                      Row(
                                        children: [
                                          const Text('Total', style: TextStyle(fontWeight: FontWeight.w800)),
                                          const Spacer(),
                                          Text(
                                            totalText,
                                            style: TextStyle(
                                              fontWeight: FontWeight.w900,
                                              color: Theme.of(context).colorScheme.primary,
                                            ),
                                          ),
                                        ],
                                      ),
                                      const SizedBox(height: 14),
                                      const Center(
                                        child: SizedBox(
                                          height: 28,
                                          width: 28,
                                          child: CircularProgressIndicator(strokeWidth: 3),
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                              if (state.errorMessage != null) ...[
                                const SizedBox(height: 10),
                                Text(
                                  state.errorMessage!,
                                  style: TextStyle(
                                    color: Theme.of(context).colorScheme.error,
                                    fontWeight: FontWeight.w800,
                                  ),
                                ),
                              ],
                            ],
                            if (state.step == CheckoutStep.success) ...[
                              Text(
                                'Número do pedido',
                                style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w900),
                              ),
                              const SizedBox(height: 6),
                              Text(
                                state.orderId == null ? '-' : '#${state.orderId}',
                                style: Theme.of(context).textTheme.displaySmall?.copyWith(fontWeight: FontWeight.w900),
                              ),
                              const SizedBox(height: 12),
                              DecoratedBox(
                                decoration: BoxDecoration(
                                  color: Theme.of(context).colorScheme.surfaceContainerHighest,
                                  borderRadius: BorderRadius.circular(14),
                                ),
                                child: Padding(
                                  padding: const EdgeInsets.all(14),
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.stretch,
                                    children: [
                                      Row(
                                        children: [
                                          const Text('Consumo', style: TextStyle(fontWeight: FontWeight.w800)),
                                          const Spacer(),
                                          Text(_fulfillmentLabel(state.fulfillment)),
                                        ],
                                      ),
                                      const SizedBox(height: 8),
                                      Row(
                                        children: [
                                          const Text('Pagamento', style: TextStyle(fontWeight: FontWeight.w800)),
                                          const Spacer(),
                                          Text(_paymentMethodLabel(state.paymentMethod)),
                                        ],
                                      ),
                                      if (state.paymentTransactionId != null) ...[
                                        const SizedBox(height: 8),
                                        Row(
                                          children: [
                                            const Text('Transação', style: TextStyle(fontWeight: FontWeight.w800)),
                                            const Spacer(),
                                            Text(state.paymentTransactionId!),
                                          ],
                                        ),
                                      ],
                                      const SizedBox(height: 12),
                                      const Divider(height: 1),
                                      const SizedBox(height: 12),
                                      Text(
                                        'Detalhes',
                                        style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w900),
                                      ),
                                      const SizedBox(height: 8),
                                      ...state.items.map(
                                        (i) {
                                          final lineTotal = i.unitPriceCents * i.quantity;
                                          return Padding(
                                            padding: const EdgeInsets.symmetric(vertical: 6),
                                            child: Row(
                                              crossAxisAlignment: CrossAxisAlignment.start,
                                              children: [
                                                Expanded(
                                                  child: Column(
                                                    crossAxisAlignment: CrossAxisAlignment.start,
                                                    children: [
                                                      Text(i.title, style: const TextStyle(fontWeight: FontWeight.w900)),
                                                      if (i.subtitle != null) ...[
                                                        const SizedBox(height: 2),
                                                        Text(
                                                          i.subtitle!,
                                                          style: TextStyle(color: Theme.of(context).colorScheme.onSurfaceVariant),
                                                        ),
                                                      ],
                                                      const SizedBox(height: 2),
                                                      Text(
                                                        '${i.quantity} x ${_formatMoney(i.unitPriceCents)}',
                                                        style: TextStyle(color: Theme.of(context).colorScheme.onSurfaceVariant),
                                                      ),
                                                    ],
                                                  ),
                                                ),
                                                const SizedBox(width: 10),
                                                Text(
                                                  _formatMoney(lineTotal),
                                                  style: const TextStyle(fontWeight: FontWeight.w900),
                                                ),
                                              ],
                                            ),
                                          );
                                        },
                                      ),
                                      const SizedBox(height: 10),
                                      Row(
                                        children: [
                                          const Text('Total', style: TextStyle(fontWeight: FontWeight.w900)),
                                          const Spacer(),
                                          Text(
                                            totalText,
                                            style: TextStyle(
                                              fontWeight: FontWeight.w900,
                                              color: Theme.of(context).colorScheme.primary,
                                            ),
                                          ),
                                        ],
                                      ),
                                    ],
                                  ),
                                ),
                              ),
                              const SizedBox(height: 14),
                              FilledButton(
                                onPressed: closeAndNotify,
                                child: const Padding(
                                  padding: EdgeInsets.symmetric(vertical: 12),
                                  child: Text('Concluir'),
                                ),
                              ),
                            ],
                          ],
                        ),
                      ),
                    ),
                    if (state.step != CheckoutStep.success) ...[
                      const SizedBox(height: 10),
                      Align(
                        alignment: Alignment.centerRight,
                        child: TextButton(
                          onPressed: state.isSubmitting ? null : () => Navigator.of(context).pop(),
                          child: const Text('Cancelar'),
                        ),
                      ),
                    ],
                  ],
                );
              },
            ),
          ),
        ),
      ),
    );
  }

}

String _formatMoney(int cents) {
  final value = cents / 100;
  return 'R\$ ${value.toStringAsFixed(2).replaceAll('.', ',')}';
}

String _fulfillmentLabel(OrderFulfillment? v) {
  return switch (v) {
    OrderFulfillment.dineIn => 'No local',
    OrderFulfillment.takeAway => 'Para levar',
    null => '-',
  };
}

String _paymentMethodLabel(PaymentMethod? v) {
  return switch (v) {
    PaymentMethod.creditCard => 'Cartão de crédito',
    PaymentMethod.debitCard => 'Cartão de débito',
    PaymentMethod.pix => 'Pix',
    PaymentMethod.cash => 'Dinheiro',
    null => '-',
  };
}

String _formatDateTime(DateTime? dt) {
  if (dt == null) return '-';
  String two(int v) => v.toString().padLeft(2, '0');
  return '${two(dt.day)}/${two(dt.month)} ${two(dt.hour)}:${two(dt.minute)}';
}

class _QrLikeBox extends StatelessWidget {
  const _QrLikeBox({
    required this.data,
    required this.size,
  });

  final String data;
  final double size;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    return DecoratedBox(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: colorScheme.outlineVariant),
      ),
      child: SizedBox(
        width: size,
        height: size,
        child: CustomPaint(
          painter: _QrLikePainter(data: data),
        ),
      ),
    );
  }
}

class _QrLikePainter extends CustomPainter {
  const _QrLikePainter({required this.data});

  final String data;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()..style = PaintingStyle.fill;
    canvas.drawRect(Offset.zero & size, paint..color = Colors.white);

    final cells = 33;
    final cellSize = size.width / cells;

    int hash = 0;
    for (final code in data.codeUnits) {
      hash = 0x1fffffff & (hash + code);
      hash = 0x1fffffff & (hash + ((0x0007ffff & hash) << 10));
      hash ^= (hash >> 6);
    }
    hash = 0x1fffffff & (hash + ((0x03ffffff & hash) << 3));
    hash ^= (hash >> 11);
    hash = 0x1fffffff & (hash + ((0x00003fff & hash) << 15));

    bool isFinder(int x, int y) {
      const s = 7;
      bool inTopLeft = x < s && y < s;
      bool inTopRight = x >= cells - s && y < s;
      bool inBottomLeft = x < s && y >= cells - s;
      return inTopLeft || inTopRight || inBottomLeft;
    }

    paint.color = Colors.black;

    for (var y = 0; y < cells; y++) {
      for (var x = 0; x < cells; x++) {
        if (isFinder(x, y)) continue;
        final bit = ((hash >> ((x + y * cells) % 31)) & 1) == 1;
        if (!bit) continue;
        canvas.drawRect(
          Rect.fromLTWH(x * cellSize, y * cellSize, cellSize, cellSize),
          paint,
        );
      }
    }

    void drawFinder(double ox, double oy) {
      final r1 = Rect.fromLTWH(ox, oy, cellSize * 7, cellSize * 7);
      final r2 = Rect.fromLTWH(ox + cellSize, oy + cellSize, cellSize * 5, cellSize * 5);
      final r3 = Rect.fromLTWH(ox + cellSize * 2, oy + cellSize * 2, cellSize * 3, cellSize * 3);
      canvas.drawRect(r1, paint);
      canvas.drawRect(r2, paint..color = Colors.white);
      canvas.drawRect(r3, paint..color = Colors.black);
    }

    drawFinder(0, 0);
    drawFinder((cells - 7) * cellSize, 0);
    drawFinder(0, (cells - 7) * cellSize);
  }

  @override
  bool shouldRepaint(covariant _QrLikePainter oldDelegate) => oldDelegate.data != data;
}
