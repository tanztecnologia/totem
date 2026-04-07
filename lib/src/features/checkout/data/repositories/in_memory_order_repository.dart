import '../../domain/entities/checkout_order.dart';
import '../../domain/repositories/order_repository.dart';

class InMemoryOrderRepository implements OrderRepository {
  InMemoryOrderRepository();

  final List<CheckoutOrder> _orders = <CheckoutOrder>[];

  @override
  Future<String> placeOrder(CheckoutOrder order) async {
    _orders.add(order);
    return (_orders.length).toString();
  }
}

