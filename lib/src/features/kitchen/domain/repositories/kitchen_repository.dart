import '../entities/kitchen_order.dart';

abstract class KitchenRepository {
  Future<List<KitchenOrder>> listOrders({List<KitchenOrderStatus>? statuses});
  Future<KitchenOrder?> getOrder(String orderId);
  Future<void> updateOrderStatus(String orderId, KitchenOrderStatus newStatus);
}
