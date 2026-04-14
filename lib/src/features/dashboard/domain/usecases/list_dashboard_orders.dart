import '../entities/dashboard_orders_page.dart';
import '../repositories/dashboard_repository.dart';

class ListDashboardOrders {
  const ListDashboardOrders(this._repository);

  final DashboardRepository _repository;

  Future<DashboardOrdersPage> call({
    int limit = 50,
    DateTime? cursorUpdatedAt,
    String? cursorOrderId,
  }) {
    return _repository.listOrdersPage(
      limit: limit,
      cursorUpdatedAt: cursorUpdatedAt,
      cursorOrderId: cursorOrderId,
    );
  }
}
