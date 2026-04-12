import '../entities/checkout_item.dart';
import '../entities/checkout_order.dart';
import '../services/checkout_service.dart';

class StartCheckout {
  const StartCheckout(this._service);

  final CheckoutService _service;

  Future<CheckoutStartResult> call({
    required List<CheckoutItem> items,
    required OrderFulfillment fulfillment,
    required PaymentMethod paymentMethod,
  }) {
    return _service.startCheckout(
      items: items,
      fulfillment: fulfillment,
      paymentMethod: paymentMethod,
    );
  }
}
