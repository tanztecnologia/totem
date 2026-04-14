import 'dashboard_order.dart';

class DashboardOrdersPage {
  const DashboardOrdersPage({
    required this.items,
    required this.nextCursorUpdatedAt,
    required this.nextCursorOrderId,
  });

  final List<DashboardOrder> items;
  final DateTime? nextCursorUpdatedAt;
  final String? nextCursorOrderId;

  bool get hasMore => nextCursorUpdatedAt != null && nextCursorOrderId != null && nextCursorOrderId!.trim().isNotEmpty;
}
