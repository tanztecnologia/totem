import '../entities/checkout_order.dart';
import '../repositories/order_repository.dart';

class PlaceOrder {
  const PlaceOrder(this._repository);

  final OrderRepository _repository;

  Future<String> call(CheckoutOrder order) {
    return _repository.placeOrder(order);
  }
}

