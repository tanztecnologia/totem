import '../entities/dashboard_orders_page.dart';
import '../entities/dashboard_overview.dart';

abstract class DashboardRepository {
  Future<DashboardOverview> getOverview({
    DateTime? fromInclusive,
    DateTime? toInclusive,
  });

  Future<DashboardOrdersPage> listOrdersPage({
    int limit = 50,
    DateTime? cursorUpdatedAt,
    String? cursorOrderId,
  });
}
