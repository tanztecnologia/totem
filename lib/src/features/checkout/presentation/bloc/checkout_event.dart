import '../../domain/entities/checkout_item.dart';
import '../../domain/entities/checkout_order.dart';

sealed class CheckoutEvent {
  const CheckoutEvent();
}

final class CheckoutStarted extends CheckoutEvent {
  const CheckoutStarted({
    required this.items,
    required this.totalCents,
    required this.totalText,
  });

  final List<CheckoutItem> items;
  final int totalCents;
  final String totalText;
}

final class CheckoutFulfillmentSelected extends CheckoutEvent {
  const CheckoutFulfillmentSelected(this.fulfillment);

  final OrderFulfillment fulfillment;
}

final class CheckoutPaymentMethodSelected extends CheckoutEvent {
  const CheckoutPaymentMethodSelected(this.paymentMethod);

  final PaymentMethod paymentMethod;
}

final class CheckoutBackRequested extends CheckoutEvent {
  const CheckoutBackRequested();
}

final class CheckoutConfirmed extends CheckoutEvent {
  const CheckoutConfirmed();
}

final class CheckoutPixPaymentConfirmed extends CheckoutEvent {
  const CheckoutPixPaymentConfirmed();
}
