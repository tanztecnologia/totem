import 'package:flutter_test/flutter_test.dart';
import 'package:totem/src/features/checkout/domain/entities/checkout_order.dart';
import 'package:totem/src/features/dashboard/domain/entities/dashboard_order.dart';
import 'package:totem/src/features/dashboard/domain/entities/dashboard_orders_page.dart';
import 'package:totem/src/features/dashboard/domain/entities/dashboard_overview.dart';
import 'package:totem/src/features/dashboard/domain/repositories/dashboard_repository.dart';
import 'package:totem/src/features/dashboard/domain/usecases/get_dashboard_overview.dart';
import 'package:totem/src/features/dashboard/domain/usecases/list_dashboard_orders.dart';
import 'package:totem/src/features/dashboard/presentation/bloc/dashboard_cubit.dart';
import 'package:totem/src/features/dashboard/presentation/bloc/dashboard_state.dart';

void main() {
  test('DashboardCubit inicia com estado inicial', () {
    final repo = _FakeDashboardRepository();
    final cubit = DashboardCubit(
      getOverview: GetDashboardOverview(repo),
      listOrders: ListDashboardOrders(repo),
    );
    addTearDown(cubit.close);

    expect(cubit.state, isA<DashboardState>());
    expect(cubit.state.overview, isNull);
    expect(cubit.state.orders, isEmpty);
  });

  test('DashboardCubit carrega overview', () async {
    final repo = _FakeDashboardRepository(
      overview: DashboardOverview(
        fromInclusive: DateTime(2025, 1, 1),
        toInclusive: DateTime(2025, 1, 7),
        ordersCount: 10,
        paidOrdersCount: 4,
        cancelledOrdersCount: 1,
        revenueCents: 10000,
        averageTicketCents: 2500,
        paymentsByMethod: const [
          DashboardPaymentMethodSummaryItem(method: PaymentMethod.pix, amountCents: 6000, paymentsCount: 3),
        ],
        paymentsByProvider: const [
          DashboardPaymentProviderSummaryItem(provider: 'TEF', amountCents: 6000, paymentsCount: 3),
        ],
        ordersByKitchenStatus: const [
          DashboardKitchenStatusSummaryItem(kitchenStatus: 'Queued', ordersCount: 5),
        ],
      ),
    );
    final cubit = DashboardCubit(
      getOverview: GetDashboardOverview(repo),
      listOrders: ListDashboardOrders(repo),
    );
    addTearDown(cubit.close);

    expectLater(
      cubit.stream,
      emitsInOrder(
        [
          isA<DashboardState>().having((s) => s.isLoadingOverview, 'isLoadingOverview', true),
          isA<DashboardState>()
              .having((s) => s.isLoadingOverview, 'isLoadingOverview', false)
              .having((s) => s.overview?.revenueCents, 'revenueCents', 10000),
        ],
      ),
    );

    await cubit.loadOverview();
  });

  test('DashboardCubit carrega pedidos recentes', () async {
    final repo = _FakeDashboardRepository(
      orders: [
        DashboardOrder(
          orderId: 'id-1',
          comanda: '10',
          status: 'Paid',
          kitchenStatus: 'Queued',
          totalCents: 2500,
          createdAt: DateTime(2025, 1, 1),
          updatedAt: DateTime(2025, 1, 1, 10),
          paymentStatus: 'Approved',
          paymentMethod: PaymentMethod.debitCard,
          paymentAmountCents: 2500,
          paymentProvider: 'POS',
        ),
      ],
    );
    final cubit = DashboardCubit(
      getOverview: GetDashboardOverview(repo),
      listOrders: ListDashboardOrders(repo),
    );
    addTearDown(cubit.close);

    expectLater(
      cubit.stream,
      emitsInOrder(
        [
          isA<DashboardState>().having((s) => s.isLoadingOrders, 'isLoadingOrders', true),
          isA<DashboardState>()
              .having((s) => s.isLoadingOrders, 'isLoadingOrders', false)
              .having((s) => s.orders.length, 'orders.length', 1),
        ],
      ),
    );

    await cubit.reloadOrders();
  });
}

class _FakeDashboardRepository implements DashboardRepository {
  _FakeDashboardRepository({
    DashboardOverview? overview,
    List<DashboardOrder>? orders,
  })  : _overview = overview,
        _orders = orders ?? const <DashboardOrder>[];

  final DashboardOverview? _overview;
  final List<DashboardOrder> _orders;

  @override
  Future<DashboardOverview> getOverview({DateTime? fromInclusive, DateTime? toInclusive}) async {
    return _overview ??
        DashboardOverview(
          fromInclusive: fromInclusive ?? DateTime(2025, 1, 1),
          toInclusive: toInclusive ?? DateTime(2025, 1, 7),
          ordersCount: 0,
          paidOrdersCount: 0,
          cancelledOrdersCount: 0,
          revenueCents: 0,
          averageTicketCents: 0,
          paymentsByMethod: const [],
          paymentsByProvider: const [],
          ordersByKitchenStatus: const [],
        );
  }

  @override
  Future<DashboardOrdersPage> listOrdersPage({
    int limit = 50,
    DateTime? cursorUpdatedAt,
    String? cursorOrderId,
  }) async {
    return DashboardOrdersPage(
      items: _orders.take(limit).toList(growable: false),
      nextCursorUpdatedAt: null,
      nextCursorOrderId: null,
    );
  }
}
