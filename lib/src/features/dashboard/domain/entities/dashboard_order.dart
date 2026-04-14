import '../../../checkout/domain/entities/checkout_order.dart';

class DashboardOrder {
  const DashboardOrder({
    required this.orderId,
    required this.comanda,
    required this.status,
    required this.kitchenStatus,
    required this.totalCents,
    required this.createdAt,
    required this.updatedAt,
    required this.paymentStatus,
    required this.paymentMethod,
    required this.paymentAmountCents,
    required this.paymentProvider,
  });

  final String orderId;
  final String? comanda;
  final String status;
  final String kitchenStatus;
  final int totalCents;
  final DateTime createdAt;
  final DateTime updatedAt;
  final String? paymentStatus;
  final PaymentMethod? paymentMethod;
  final int? paymentAmountCents;
  final String? paymentProvider;
}
