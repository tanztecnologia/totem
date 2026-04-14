import '../../domain/entities/dashboard_order.dart';
import '../../domain/entities/dashboard_overview.dart';

class DashboardState {
  const DashboardState({
    required this.fromInclusive,
    required this.toInclusive,
    required this.overview,
    required this.orders,
    required this.isLoadingOverview,
    required this.isLoadingOrders,
    required this.isLoadingMoreOrders,
    required this.nextCursorUpdatedAt,
    required this.nextCursorOrderId,
    required this.errorMessage,
  });

  factory DashboardState.initial() {
    final now = DateTime.now();
    final from = now.subtract(const Duration(days: 7));
    return DashboardState(
      fromInclusive: DateTime(from.year, from.month, from.day),
      toInclusive: DateTime(now.year, now.month, now.day, 23, 59, 59),
      overview: null,
      orders: const <DashboardOrder>[],
      isLoadingOverview: false,
      isLoadingOrders: false,
      isLoadingMoreOrders: false,
      nextCursorUpdatedAt: null,
      nextCursorOrderId: null,
      errorMessage: null,
    );
  }

  final DateTime fromInclusive;
  final DateTime toInclusive;
  final DashboardOverview? overview;
  final List<DashboardOrder> orders;
  final bool isLoadingOverview;
  final bool isLoadingOrders;
  final bool isLoadingMoreOrders;
  final DateTime? nextCursorUpdatedAt;
  final String? nextCursorOrderId;
  final String? errorMessage;

  bool get isLoading => isLoadingOverview || isLoadingOrders || isLoadingMoreOrders;
  bool get canLoadMore =>
      !isLoadingMoreOrders &&
      nextCursorUpdatedAt != null &&
      nextCursorOrderId != null &&
      nextCursorOrderId!.trim().isNotEmpty;

  DashboardState copyWith({
    DateTime? fromInclusive,
    DateTime? toInclusive,
    DashboardOverview? overview,
    List<DashboardOrder>? orders,
    bool? isLoadingOverview,
    bool? isLoadingOrders,
    bool? isLoadingMoreOrders,
    DateTime? nextCursorUpdatedAt,
    String? nextCursorOrderId,
    String? errorMessage,
  }) {
    return DashboardState(
      fromInclusive: fromInclusive ?? this.fromInclusive,
      toInclusive: toInclusive ?? this.toInclusive,
      overview: overview ?? this.overview,
      orders: orders ?? this.orders,
      isLoadingOverview: isLoadingOverview ?? this.isLoadingOverview,
      isLoadingOrders: isLoadingOrders ?? this.isLoadingOrders,
      isLoadingMoreOrders: isLoadingMoreOrders ?? this.isLoadingMoreOrders,
      nextCursorUpdatedAt: nextCursorUpdatedAt ?? this.nextCursorUpdatedAt,
      nextCursorOrderId: nextCursorOrderId ?? this.nextCursorOrderId,
      errorMessage: errorMessage,
    );
  }
}
