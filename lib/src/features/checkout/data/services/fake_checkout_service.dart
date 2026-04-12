import 'dart:math';

import '../../domain/entities/checkout_item.dart';
import '../../domain/entities/checkout_order.dart';
import '../../domain/entities/payment_result.dart';
import '../../domain/entities/pix_charge.dart';
import '../../domain/services/checkout_service.dart';

class FakeCheckoutService implements CheckoutService {
  FakeCheckoutService();

  @override
  Future<CheckoutStartResult> startCheckout({
    required List<CheckoutItem> items,
    required OrderFulfillment fulfillment,
    required PaymentMethod paymentMethod,
    String? comanda,
  }) async {
    await Future<void>.delayed(const Duration(milliseconds: 250));
    final orderId = _randomId();
    final paymentId = _randomId();
    if (paymentMethod == PaymentMethod.pix) {
      final now = DateTime.now();
      final expiresAt = now.add(const Duration(minutes: 5));
      final payload = '000201|REF=order-$orderId|EXPIRES=${expiresAt.toIso8601String()}';
      return CheckoutStartResult(
        orderId: orderId,
        paymentId: paymentId,
        pixCharge: PixCharge(
          amountCents: items.fold<int>(0, (acc, i) => acc + (i.unitPriceCents * i.quantity)),
          payload: payload,
          expiresAt: expiresAt,
          reference: 'order-$orderId',
        ),
      );
    }

    return CheckoutStartResult(orderId: orderId, paymentId: paymentId, pixCharge: null);
  }

  @override
  Future<PaymentResult> confirmPayment({
    required String paymentId,
  }) async {
    await Future<void>.delayed(const Duration(milliseconds: 600));
    return PaymentResult(
      isApproved: true,
      transactionId: DateTime.now().millisecondsSinceEpoch.toString(),
      message: 'APROVADO',
    );
  }
}

String _randomId() {
  final r = Random();
  final parts = List<String>.generate(8, (_) => r.nextInt(16).toRadixString(16)).join();
  return 'id-$parts';
}
