import 'package:flutter_test/flutter_test.dart';
import 'package:totem/src/features/checkout/domain/entities/checkout_item.dart';
import 'package:totem/src/features/checkout/domain/entities/checkout_order.dart';
import 'package:totem/src/features/checkout/domain/entities/payment_result.dart';
import 'package:totem/src/features/checkout/domain/entities/pix_charge.dart';
import 'package:totem/src/features/checkout/domain/repositories/order_repository.dart';
import 'package:totem/src/features/checkout/domain/services/payment_service.dart';
import 'package:totem/src/features/checkout/domain/usecases/create_pix_charge.dart';
import 'package:totem/src/features/checkout/domain/usecases/process_payment.dart';
import 'package:totem/src/features/checkout/domain/usecases/place_order.dart';
import 'package:totem/src/features/checkout/presentation/bloc/checkout_bloc.dart';

void main() {
  test('CheckoutBloc finaliza com sucesso após pagamento aprovado (cartão)', () async {
    final orderRepository = _FakeOrderRepository();
    final paymentService = _PaymentServiceApproved();
    final bloc = CheckoutBloc(
      placeOrder: PlaceOrder(orderRepository),
      createPixCharge: CreatePixCharge(paymentService),
      processPayment: ProcessPayment(paymentService),
    );
    addTearDown(bloc.close);

    bloc.add(
      const CheckoutStarted(
        items: <CheckoutItem>[
          CheckoutItem(
            id: 'line-1',
            title: 'Hambúrguer',
            quantity: 2,
            unitPriceCents: 1000,
          ),
        ],
        totalCents: 2000,
        totalText: 'R\$ 20,00',
      ),
    );
    await Future<void>.delayed(Duration.zero);
    expect(bloc.state.step, CheckoutStep.fulfillment);

    bloc.add(const CheckoutFulfillmentSelected(OrderFulfillment.dineIn));
    await Future<void>.delayed(Duration.zero);
    expect(bloc.state.step, CheckoutStep.payment);
    expect(bloc.state.fulfillment, OrderFulfillment.dineIn);

    bloc.add(const CheckoutPaymentMethodSelected(PaymentMethod.creditCard));
    await Future<void>.delayed(Duration.zero);
    expect(bloc.state.paymentMethod, PaymentMethod.creditCard);

    bloc.add(const CheckoutConfirmed());
    await bloc.stream.firstWhere((s) => s.isSuccess);

    expect(orderRepository.orders, hasLength(1));
    final saved = orderRepository.orders.single;
    expect(saved.totalCents, 2000);
    expect(saved.fulfillment, OrderFulfillment.dineIn);
    expect(saved.paymentMethod, PaymentMethod.creditCard);
  });

  test('CheckoutBloc não finaliza quando pagamento é negado', () async {
    final orderRepository = _FakeOrderRepository();
    final paymentService = _PaymentServiceDeclined();
    final bloc = CheckoutBloc(
      placeOrder: PlaceOrder(orderRepository),
      createPixCharge: CreatePixCharge(paymentService),
      processPayment: ProcessPayment(paymentService),
    );
    addTearDown(bloc.close);

    bloc.add(
      const CheckoutStarted(
        items: <CheckoutItem>[
          CheckoutItem(
            id: 'line-1',
            title: 'Hambúrguer',
            quantity: 1,
            unitPriceCents: 1000,
          ),
        ],
        totalCents: 1000,
        totalText: 'R\$ 10,00',
      ),
    );
    await Future<void>.delayed(Duration.zero);

    bloc.add(const CheckoutFulfillmentSelected(OrderFulfillment.takeAway));
    await Future<void>.delayed(Duration.zero);

    bloc.add(const CheckoutPaymentMethodSelected(PaymentMethod.creditCard));
    await Future<void>.delayed(Duration.zero);

    bloc.add(const CheckoutConfirmed());
    await bloc.stream.firstWhere((s) => !s.isSubmitting && s.errorMessage != null);

    expect(bloc.state.isSuccess, false);
    expect(orderRepository.orders, isEmpty);
  });

  test('CheckoutBloc ignora confirmação sem seleção completa', () async {
    final orderRepository = _FakeOrderRepository();
    final paymentService = _PaymentServiceApproved();
    final bloc = CheckoutBloc(
      placeOrder: PlaceOrder(orderRepository),
      createPixCharge: CreatePixCharge(paymentService),
      processPayment: ProcessPayment(paymentService),
    );
    addTearDown(bloc.close);

    bloc.add(
      const CheckoutStarted(
        items: <CheckoutItem>[
          CheckoutItem(
            id: 'line-1',
            title: 'Hambúrguer',
            quantity: 1,
            unitPriceCents: 1000,
          ),
        ],
        totalCents: 1000,
        totalText: 'R\$ 10,00',
      ),
    );
    await Future<void>.delayed(Duration.zero);

    bloc.add(const CheckoutConfirmed());
    await Future<void>.delayed(Duration.zero);

    expect(bloc.state.isSubmitting, false);
    expect(bloc.state.isSuccess, false);
    expect(orderRepository.orders, isEmpty);
  });

  test('CheckoutBloc no Pix gera cobrança e só finaliza após confirmar pagamento', () async {
    final orderRepository = _FakeOrderRepository();
    final paymentService = _PaymentServiceApproved();
    final bloc = CheckoutBloc(
      placeOrder: PlaceOrder(orderRepository),
      createPixCharge: CreatePixCharge(paymentService),
      processPayment: ProcessPayment(paymentService),
    );
    addTearDown(bloc.close);

    bloc.add(
      const CheckoutStarted(
        items: <CheckoutItem>[
          CheckoutItem(
            id: 'line-1',
            title: 'Hambúrguer',
            quantity: 2,
            unitPriceCents: 1000,
          ),
        ],
        totalCents: 2000,
        totalText: 'R\$ 20,00',
      ),
    );
    await Future<void>.delayed(Duration.zero);

    bloc.add(const CheckoutFulfillmentSelected(OrderFulfillment.takeAway));
    await Future<void>.delayed(Duration.zero);

    bloc.add(const CheckoutPaymentMethodSelected(PaymentMethod.pix));
    await Future<void>.delayed(Duration.zero);

    bloc.add(const CheckoutConfirmed());
    await bloc.stream.firstWhere((s) => s.step == CheckoutStep.pixQr && s.pixCharge != null && !s.isSubmitting);

    expect(bloc.state.isSuccess, false);
    expect(orderRepository.orders, isEmpty);

    bloc.add(const CheckoutPixPaymentConfirmed());
    await bloc.stream.firstWhere((s) => s.isSuccess);

    expect(orderRepository.orders, hasLength(1));
    expect(orderRepository.orders.single.paymentMethod, PaymentMethod.pix);
  });
}

class _FakeOrderRepository implements OrderRepository {
  final List<CheckoutOrder> orders = <CheckoutOrder>[];

  @override
  Future<String> placeOrder(CheckoutOrder order) async {
    orders.add(order);
    return orders.length.toString();
  }
}

class _PaymentServiceApproved implements PaymentService {
  @override
  Future<PixCharge> createPixCharge({
    required int amountCents,
    required String reference,
  }) async {
    return PixCharge(
      amountCents: amountCents,
      payload: 'payload-$reference',
      expiresAt: DateTime.now().add(const Duration(minutes: 5)),
      reference: reference,
    );
  }

  @override
  Future<PaymentResult> pay({
    required int amountCents,
    required PaymentMethod method,
  }) async {
    return const PaymentResult(isApproved: true, transactionId: 'tx-1', message: 'APROVADO');
  }
}

class _PaymentServiceDeclined implements PaymentService {
  @override
  Future<PixCharge> createPixCharge({
    required int amountCents,
    required String reference,
  }) async {
    return PixCharge(
      amountCents: amountCents,
      payload: 'payload-$reference',
      expiresAt: DateTime.now().add(const Duration(minutes: 5)),
      reference: reference,
    );
  }

  @override
  Future<PaymentResult> pay({
    required int amountCents,
    required PaymentMethod method,
  }) async {
    return const PaymentResult(isApproved: false, message: 'NEGADO');
  }
}
