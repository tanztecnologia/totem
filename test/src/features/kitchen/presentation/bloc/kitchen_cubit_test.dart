import 'package:flutter_test/flutter_test.dart';
import 'package:totem/src/features/kitchen/domain/entities/kitchen_order.dart';
import 'package:totem/src/features/kitchen/domain/repositories/kitchen_repository.dart';
import 'package:totem/src/features/kitchen/presentation/bloc/kitchen_cubit.dart';

class FakeKitchenRepository implements KitchenRepository {
  List<KitchenOrder> orders = [];
  bool throwError = false;

  @override
  Future<KitchenOrder?> getOrder(String orderId) async {
    return orders.firstWhere((o) => o.id == orderId);
  }

  @override
  Future<List<KitchenOrder>> listOrders({List<KitchenOrderStatus>? statuses}) async {
    if (throwError) throw Exception('API Error');
    if (statuses == null) return orders;
    return orders.where((o) => statuses.contains(o.status)).toList();
  }

  @override
  Future<void> updateOrderStatus(String orderId, KitchenOrderStatus newStatus) async {
    if (throwError) throw Exception('Update Error');
    final idx = orders.indexWhere((o) => o.id == orderId);
    if (idx >= 0) {
      final old = orders[idx];
      orders[idx] = KitchenOrder(
        id: old.id,
        status: newStatus,
        fulfillment: old.fulfillment,
        createdAt: old.createdAt,
        updatedAt: DateTime.now(),
        items: old.items,
      );
    }
  }
}

void main() {
  late KitchenCubit cubit;
  late FakeKitchenRepository repository;

  setUp(() {
    repository = FakeKitchenRepository();
    cubit = KitchenCubit(repository);
  });

  tearDown(() {
    cubit.close();
  });

  test('deve inicializar com KitchenInitial', () {
    expect(cubit.state, isA<KitchenInitial>());
  });

  test('deve carregar pedidos com sucesso e agrupar por status', () async {
    repository.orders = [
      KitchenOrder(
        id: '1',
        status: KitchenOrderStatus.queued,
        fulfillment: 'DineIn',
        createdAt: DateTime.now(),
        updatedAt: DateTime.now(),
        items: const [],
      ),
      KitchenOrder(
        id: '2',
        status: KitchenOrderStatus.inPreparation,
        fulfillment: 'DineIn',
        createdAt: DateTime.now(),
        updatedAt: DateTime.now(),
        items: const [],
      ),
    ];

    await cubit.loadOrders();

    final state = cubit.state;
    expect(state, isA<KitchenLoaded>());
    if (state is KitchenLoaded) {
      expect(state.queued.length, 1);
      expect(state.inPreparation.length, 1);
      expect(state.ready.length, 0);
    }
  });

  test('deve emitir erro caso repositório falhe', () async {
    repository.throwError = true;

    await cubit.loadOrders();

    expect(cubit.state, isA<KitchenError>());
  });
}
