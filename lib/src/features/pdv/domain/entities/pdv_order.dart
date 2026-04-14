class PdvOrder {
  const PdvOrder({
    required this.orderId,
    required this.comanda,
    required this.status,
    required this.kitchenStatus,
    required this.totalCents,
    required this.createdAt,
    required this.updatedAt,
  });

  final String orderId;
  final String comanda;
  final String status;
  final String kitchenStatus;
  final int totalCents;
  final DateTime createdAt;
  final DateTime updatedAt;

  bool get isPaid => status == 'Paid';
}

