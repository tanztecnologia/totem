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
  final DateTime? queuedAt;
  final DateTime? inPreparationAt;
  final DateTime? readyAt;
  final DateTime? completedAt;
  final DateTime? cancelledAt;
  final int currentStageElapsedSeconds;
  final int currentStageTargetSeconds;
  final bool isOverdue;
  final List<KitchenOrderItem> items;

  const KitchenOrder({
    required this.id,
    required this.status,
    required this.fulfillment,
    required this.createdAt,
    required this.updatedAt,
    required this.queuedAt,
    required this.inPreparationAt,
    required this.readyAt,
    required this.completedAt,
    required this.cancelledAt,
    required this.currentStageElapsedSeconds,
    required this.currentStageTargetSeconds,
    required this.isOverdue,
    required this.items,
  });

  factory KitchenOrder.fromJson(Map<String, dynamic> json) {
    DateTime? parseDateTimeNullable(Object? raw) {
      if (raw is! String) return null;
      final v = raw.trim();
      if (v.isEmpty) return null;
      return DateTime.parse(v).toLocal();
    }

    return KitchenOrder(
      id: json['orderId'] as String,
      status: KitchenOrderStatusX.fromString(json['kitchenStatus'] as String),
      fulfillment: json['fulfillment'] as String,
      createdAt: DateTime.parse(json['createdAt'] as String).toLocal(),
      updatedAt: DateTime.parse(json['updatedAt'] as String).toLocal(),
      queuedAt: parseDateTimeNullable(json['queuedAt']),
      inPreparationAt: parseDateTimeNullable(json['inPreparationAt']),
      readyAt: parseDateTimeNullable(json['readyAt']),
      completedAt: parseDateTimeNullable(json['completedAt']),
      cancelledAt: parseDateTimeNullable(json['cancelledAt']),
      currentStageElapsedSeconds: (json['currentStageElapsedSeconds'] as num?)?.toInt() ?? 0,
      currentStageTargetSeconds: (json['currentStageTargetSeconds'] as num?)?.toInt() ?? 0,
      isOverdue: (json['isOverdue'] as bool?) ?? false,
      items: (json['items'] as List<dynamic>?)
              ?.map((e) => KitchenOrderItem.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const [],
    );
  }
}
