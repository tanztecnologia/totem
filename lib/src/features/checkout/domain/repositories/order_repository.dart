import '../entities/checkout_order.dart';

abstract class OrderRepository {
  Future<String> placeOrder(CheckoutOrder order);
}

