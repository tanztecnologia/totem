import 'checkout_item.dart';

enum OrderFulfillment {
  dineIn,
  takeAway,
}

enum PaymentMethod {
  creditCard,
  debitCard,
  pix,
  cash,
}

class CheckoutOrder {
  const CheckoutOrder({
    required this.items,
    required this.totalCents,
    required this.fulfillment,
    required this.paymentMethod,
  });

  final List<CheckoutItem> items;
  final int totalCents;
  final OrderFulfillment fulfillment;
  final PaymentMethod paymentMethod;
}
