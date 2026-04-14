import '../../../checkout/domain/entities/checkout_order.dart';

class DashboardOverview {
  const DashboardOverview({
    required this.fromInclusive,
    required this.toInclusive,
    required this.ordersCount,
    required this.paidOrdersCount,
    required this.cancelledOrdersCount,
    required this.revenueCents,
    required this.averageTicketCents,
    required this.paymentsByMethod,
    required this.paymentsByProvider,
    required this.ordersByKitchenStatus,
  });

  final DateTime fromInclusive;
  final DateTime toInclusive;
  final int ordersCount;
  final int paidOrdersCount;
  final int cancelledOrdersCount;
  final int revenueCents;
  final int averageTicketCents;
  final List<DashboardPaymentMethodSummaryItem> paymentsByMethod;
  final List<DashboardPaymentProviderSummaryItem> paymentsByProvider;
  final List<DashboardKitchenStatusSummaryItem> ordersByKitchenStatus;
}

class DashboardPaymentMethodSummaryItem {
  const DashboardPaymentMethodSummaryItem({
    required this.method,
    required this.amountCents,
    required this.paymentsCount,
  });

  final PaymentMethod method;
  final int amountCents;
  final int paymentsCount;
}

class DashboardKitchenStatusSummaryItem {
  const DashboardKitchenStatusSummaryItem({
    required this.kitchenStatus,
    required this.ordersCount,
  });

  final String kitchenStatus;
  final int ordersCount;
}

class DashboardPaymentProviderSummaryItem {
  const DashboardPaymentProviderSummaryItem({
    required this.provider,
    required this.amountCents,
    required this.paymentsCount,
  });

  final String provider;
  final int amountCents;
  final int paymentsCount;
}
