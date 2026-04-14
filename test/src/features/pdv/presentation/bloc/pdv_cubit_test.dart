import 'package:flutter_test/flutter_test.dart';
import 'package:totem/src/features/checkout/domain/entities/checkout_order.dart';
import 'package:totem/src/features/pdv/domain/entities/pdv_order.dart';
import 'package:totem/src/features/pdv/domain/repositories/pdv_repository.dart';
import 'package:totem/src/features/pdv/domain/usecases/list_pdv_orders_by_comanda.dart';
import 'package:totem/src/features/pdv/domain/usecases/pay_pdv_order.dart';
import 'package:totem/src/features/pdv/presentation/bloc/pdv_cubit.dart';
import 'package:totem/src/features/pdv/presentation/bloc/pdv_state.dart';

void main() {
  test('PdvCubit inicia com estado inicial', () {
    final repo = _FakePdvRepository();
    final cubit = PdvCubit(
      repository: repo,
      listOrdersByComanda: ListPdvOrdersByComanda(repo),
      payOrder: PayPdvOrder(repo),
    );
    addTearDown(cubit.close);

    expect(cubit.state, isA<PdvState>());
    expect(cubit.state.orders, isEmpty);
  });

  test('PdvCubit busca pedidos por comanda', () async {
    final repo = _FakePdvRepository(
      orders: [
        PdvOrder(
          orderId: 'id-1',
          comanda: '10',
          status: 'Created',
          kitchenStatus: 'Queued',
          totalCents: 2500,
          createdAt: DateTime(2025, 1, 1),
          updatedAt: DateTime(2025, 1, 1),
        ),
      ],
    );
    final cubit = PdvCubit(
      repository: repo,
      listOrdersByComanda: ListPdvOrdersByComanda(repo),
      payOrder: PayPdvOrder(repo),
    );
    addTearDown(cubit.close);

    expectLater(
      cubit.stream,
      emitsInOrder(
        [
          isA<PdvState>().having((s) => s.comanda, 'comanda', '10'),
          isA<PdvState>().having((s) => s.isLoading, 'isLoading', true),
          isA<PdvState>()
              .having((s) => s.isLoading, 'isLoading', false)
              .having((s) => s.orders.length, 'orders.length', 1),
        ],
      ),
    );

    cubit.setComanda('10');
    await cubit.search();
  });

  test('PdvCubit paga pedido e atualiza lista', () async {
    final repo = _FakePdvRepository(
      orders: [
        PdvOrder(
          orderId: 'id-1',
          comanda: '10',
          status: 'Created',
          kitchenStatus: 'Queued',
          totalCents: 2500,
          createdAt: DateTime(2025, 1, 1),
          updatedAt: DateTime(2025, 1, 1),
        ),
      ],
      paymentResult: const PdvPaymentResult(
        orderId: 'id-1',
        orderStatus: 'Paid',
        kitchenStatus: 'Queued',
        paymentStatus: 'Approved',
        transactionId: 'PDV-123',
      ),
    );
    final cubit = PdvCubit(
      repository: repo,
      listOrdersByComanda: ListPdvOrdersByComanda(repo),
      payOrder: PayPdvOrder(repo),
    );
    addTearDown(cubit.close);

    await cubit.openCashRegister(openingCashCents: 0);
    cubit.setComanda('10');
    await cubit.search();

    expectLater(
      cubit.stream,
      emitsThrough(
        isA<PdvState>()
            .having((s) => s.lastPayment?.transactionId, 'tx', 'PDV-123')
            .having((s) => s.lastPayment?.isApproved, 'approved', true),
      ),
    );

    await cubit.pay(orderId: 'id-1', method: PaymentMethod.debitCard, transactionId: 'PDV-123');
  });

  test('PdvCubit bloqueia pagamento quando caixa está fechado', () async {
    final repo = _FakePdvRepository(openShift: null);
    final cubit = PdvCubit(
      repository: repo,
      listOrdersByComanda: ListPdvOrdersByComanda(repo),
      payOrder: PayPdvOrder(repo),
    );
    addTearDown(cubit.close);

    expectLater(
      cubit.stream,
      emitsThrough(isA<PdvState>().having((s) => s.errorMessage, 'errorMessage', contains('Caixa fechado'))),
    );

    await cubit.pay(orderId: 'id-1', method: PaymentMethod.cash);
  });
}

class _FakePdvRepository implements PdvRepository {
  _FakePdvRepository({
    List<PdvOrder>? orders,
    PdvPaymentResult? paymentResult,
    PdvCashRegisterShift? openShift,
  })  : _orders = orders ?? const <PdvOrder>[],
        _paymentResult = paymentResult ??
            const PdvPaymentResult(
              orderId: 'id-1',
              orderStatus: 'Paid',
              kitchenStatus: 'Queued',
              paymentStatus: 'Approved',
              transactionId: 'tx',
            ),
        _openShift = openShift;

  final List<PdvOrder> _orders;
  final PdvPaymentResult _paymentResult;
  PdvCashRegisterShift? _openShift;

  @override
  Future<PdvCashRegisterShift?> getCurrentCashRegisterShift() async {
    return _openShift;
  }

  @override
  Future<PdvCashRegisterShift> openCashRegisterShift({required int openingCashCents}) async {
    _openShift = PdvCashRegisterShift(
      id: 'shift-1',
      status: PdvCashRegisterShiftStatus.open,
      openedByEmail: 'pdv@empresax.local',
      openingCashCents: openingCashCents,
      openedAt: DateTime(2025, 1, 1),
      closingCashCents: null,
      totalSalesCents: null,
      totalCashSalesCents: null,
      expectedCashCents: null,
      closedAt: null,
    );
    return _openShift!;
  }

  @override
  Future<PdvCloseCashRegisterResult> closeCashRegisterShift({required int closingCashCents}) async {
    final shift = _openShift ??
        PdvCashRegisterShift(
          id: 'shift-1',
          status: PdvCashRegisterShiftStatus.open,
          openedByEmail: 'pdv@empresax.local',
          openingCashCents: 0,
          openedAt: DateTime(2025, 1, 1),
          closingCashCents: null,
          totalSalesCents: null,
          totalCashSalesCents: null,
          expectedCashCents: null,
          closedAt: null,
        );

    _openShift = null;
    return PdvCloseCashRegisterResult(
      shift: shift,
      totalSalesCents: 0,
      totalCashSalesCents: 0,
      expectedCashCents: shift.openingCashCents,
      closingCashCents: closingCashCents,
      differenceCents: closingCashCents - shift.openingCashCents,
      payments: const [],
    );
  }

  @override
  Future<List<PdvOrder>> listOrdersByComanda({
    required String comanda,
    required bool includePaid,
    int limit = 50,
  }) async {
    return _orders.where((o) => o.comanda == comanda).toList(growable: false);
  }

  @override
  Future<PdvPaymentResult> payOrder({
    required String orderId,
    required PaymentMethod paymentMethod,
    String? transactionId,
  }) async {
    return _paymentResult;
  }
}
