import '../entities/checkout_item.dart';
import '../entities/checkout_order.dart';
import '../entities/payment_result.dart';
import '../entities/pix_charge.dart';

class CheckoutStartResult {
  const CheckoutStartResult({
    required this.orderId,
    required this.paymentId,
    required this.pixCharge,
  });

  final String orderId;
  final String paymentId;
  final PixCharge? pixCharge;
}

abstract class CheckoutService {
  Future<CheckoutStartResult> startCheckout({
    required List<CheckoutItem> items,
    required OrderFulfillment fulfillment,
    required PaymentMethod paymentMethod,
    String? comanda,
  });

  Future<PaymentResult> confirmPayment({
    required String paymentId,
  });
}
