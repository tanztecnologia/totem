enum KitchenOrderStatus {
  pendingPayment,
  queued,
  inPreparation,
  ready,
  completed,
  cancelled,
}

extension KitchenOrderStatusX on KitchenOrderStatus {
  static KitchenOrderStatus fromString(String value) {
    return KitchenOrderStatus.values.firstWhere(
      (e) => e.name.toLowerCase() == value.toLowerCase(),
      orElse: () => KitchenOrderStatus.queued,
    );
  }

  String get label {
    switch (this) {
      case KitchenOrderStatus.pendingPayment:
        return 'Aguardando Pagto';
      case KitchenOrderStatus.queued:
        return 'Na Fila';
      case KitchenOrderStatus.inPreparation:
        return 'Em Preparo';
      case KitchenOrderStatus.ready:
        return 'Pronto';
      case KitchenOrderStatus.completed:
        return 'Entregue';
      case KitchenOrderStatus.cancelled:
        return 'Cancelado';
    }
  }
}

class KitchenOrderItem {
  final String skuId;
  final String code;
  final String name;
  final int quantity;

  const KitchenOrderItem({
    required this.skuId,
    required this.code,
    required this.name,
    required this.quantity,
  });

  factory KitchenOrderItem.fromJson(Map<String, dynamic> json) {
    return KitchenOrderItem(
      skuId: json['skuId'] as String,
      code: json['code'] as String,
      name: json['name'] as String,
      quantity: json['quantity'] as int,
    );
  }
}

class KitchenOrder {
  final String id;
  final KitchenOrderStatus status;
  final String fulfillment;
  final DateTime createdAt;
  final DateTime updatedAt;
  final List<KitchenOrderItem> items;

  const KitchenOrder({
    required this.id,
    required this.status,
    required this.fulfillment,
    required this.createdAt,
    required this.updatedAt,
    required this.items,
  });

  factory KitchenOrder.fromJson(Map<String, dynamic> json) {
    return KitchenOrder(
      id: json['orderId'] as String,
      status: KitchenOrderStatusX.fromString(json['kitchenStatus'] as String),
      fulfillment: json['fulfillment'] as String,
      createdAt: DateTime.parse(json['createdAt'] as String),
      updatedAt: DateTime.parse(json['updatedAt'] as String),
      items: (json['items'] as List<dynamic>?)
              ?.map((e) => KitchenOrderItem.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const [],
    );
  }
}
