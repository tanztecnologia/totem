import 'dart:async';

import '../../domain/entities/kitchen_order.dart';
import '../../domain/repositories/kitchen_repository.dart';

class InMemoryKitchenRepository implements KitchenRepository {
  final List<KitchenOrder> _orders = [
    KitchenOrder(
      id: 'ord-1',
      status: KitchenOrderStatus.queued,
      fulfillment: 'DineIn',
      createdAt: DateTime.now().subtract(const Duration(minutes: 10)),
      updatedAt: DateTime.now().subtract(const Duration(minutes: 10)),
      items: [
        const KitchenOrderItem(skuId: 'sku-1', code: 'HAMB-01', name: 'X-Salada', quantity: 2),
        const KitchenOrderItem(skuId: 'sku-2', code: 'BEB-01', name: 'Coca-Cola', quantity: 1),
      ],
    ),
    KitchenOrder(
      id: 'ord-2',
      status: KitchenOrderStatus.inPreparation,
      fulfillment: 'TakeAway',
      createdAt: DateTime.now().subtract(const Duration(minutes: 5)),
      updatedAt: DateTime.now().subtract(const Duration(minutes: 2)),
      items: [
        const KitchenOrderItem(skuId: 'sku-3', code: 'HAMB-02', name: 'X-Bacon', quantity: 1),
        const KitchenOrderItem(skuId: 'sku-4', code: 'ACOM-01', name: 'Fritas Média', quantity: 1),
      ],
    ),
    KitchenOrder(
      id: 'ord-3',
      status: KitchenOrderStatus.ready,
      fulfillment: 'DineIn',
      createdAt: DateTime.now().subtract(const Duration(minutes: 15)),
      updatedAt: DateTime.now().subtract(const Duration(minutes: 1)),
      items: [
        const KitchenOrderItem(skuId: 'sku-5', code: 'HAMB-03', name: 'X-Tudo', quantity: 1),
      ],
    ),
  ];

  @override
  Future<KitchenOrder?> getOrder(String orderId) async {
    await Future.delayed(const Duration(milliseconds: 300));
    try {
      return _orders.firstWhere((o) => o.id == orderId);
    } catch (_) {
      return null;
    }
  }

  @override
  Future<List<KitchenOrder>> listOrders({List<KitchenOrderStatus>? statuses}) async {
    await Future.delayed(const Duration(milliseconds: 500));
    if (statuses == null || statuses.isEmpty) {
      return List.unmodifiable(_orders);
    }
    return _orders.where((o) => statuses.contains(o.status)).toList();
  }

  @override
  Future<void> updateOrderStatus(String orderId, KitchenOrderStatus newStatus) async {
    await Future.delayed(const Duration(milliseconds: 300));
    final index = _orders.indexWhere((o) => o.id == orderId);
    if (index >= 0) {
      final old = _orders[index];
      _orders[index] = KitchenOrder(
        id: old.id,
        status: newStatus,
        fulfillment: old.fulfillment,
        createdAt: old.createdAt,
        updatedAt: DateTime.now(),
        items: old.items,
      );
    } else {
      throw Exception('Pedido não encontrado');
    }
  }
}
