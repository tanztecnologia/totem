import 'package:flutter_bloc/flutter_bloc.dart';

import '../../domain/usecases/get_dashboard_overview.dart';
import '../../domain/usecases/list_dashboard_orders.dart';
import 'dashboard_state.dart';

class DashboardCubit extends Cubit<DashboardState> {
  DashboardCubit({
    required GetDashboardOverview getOverview,
    required ListDashboardOrders listOrders,
  })  : _getOverview = getOverview,
        _listOrders = listOrders,
        super(DashboardState.initial());

  final GetDashboardOverview _getOverview;
  final ListDashboardOrders _listOrders;

  Future<void> setDateRange({
    required DateTime fromInclusive,
    required DateTime toInclusive,
  }) async {
    emit(
      state.copyWith(
        fromInclusive: fromInclusive,
        toInclusive: toInclusive,
        orders: const [],
        nextCursorUpdatedAt: null,
        nextCursorOrderId: null,
        errorMessage: null,
      ),
    );
    await refreshAll();
  }

  Future<void> refreshAll() async {
    await Future.wait([loadOverview(), reloadOrders()]);
  }

  Future<void> loadOverview() async {
    if (state.isLoadingOverview) return;
    emit(state.copyWith(isLoadingOverview: true, errorMessage: null));
    try {
      final overview = await _getOverview(
        fromInclusive: state.fromInclusive,
        toInclusive: state.toInclusive,
      );
      emit(state.copyWith(isLoadingOverview: false, overview: overview));
    } catch (e) {
      emit(state.copyWith(isLoadingOverview: false, errorMessage: e.toString()));
    }
  }

  Future<void> reloadOrders() async {
    if (state.isLoadingOrders) return;
    emit(
      state.copyWith(
        isLoadingOrders: true,
        errorMessage: null,
        orders: const [],
        nextCursorUpdatedAt: null,
        nextCursorOrderId: null,
      ),
    );
    try {
      final page = await _listOrders(limit: 50);
      emit(
        state.copyWith(
          isLoadingOrders: false,
          orders: page.items,
          nextCursorUpdatedAt: page.nextCursorUpdatedAt,
          nextCursorOrderId: page.nextCursorOrderId,
        ),
      );
    } catch (e) {
      emit(state.copyWith(isLoadingOrders: false, errorMessage: e.toString(), orders: const []));
    }
  }

  Future<void> loadMoreOrders() async {
    if (!state.canLoadMore) return;
    emit(state.copyWith(isLoadingMoreOrders: true, errorMessage: null));
    try {
      final page = await _listOrders(
        limit: 50,
        cursorUpdatedAt: state.nextCursorUpdatedAt,
        cursorOrderId: state.nextCursorOrderId,
      );
      emit(
        state.copyWith(
          isLoadingMoreOrders: false,
          orders: [...state.orders, ...page.items],
          nextCursorUpdatedAt: page.nextCursorUpdatedAt,
          nextCursorOrderId: page.nextCursorOrderId,
        ),
      );
    } catch (e) {
      emit(state.copyWith(isLoadingMoreOrders: false, errorMessage: e.toString()));
    }
  }
}
