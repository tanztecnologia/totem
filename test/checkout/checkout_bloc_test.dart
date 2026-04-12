import 'package:flutter_test/flutter_test.dart';
import 'package:totem/src/features/checkout/domain/entities/checkout_item.dart';
import 'package:totem/src/features/checkout/domain/entities/checkout_order.dart';
import 'package:totem/src/features/checkout/domain/entities/payment_result.dart';
import 'package:totem/src/features/checkout/domain/entities/pix_charge.dart';
import 'package:totem/src/features/checkout/domain/services/checkout_service.dart';
import 'package:totem/src/features/checkout/domain/usecases/confirm_payment.dart';
import 'package:totem/src/features/checkout/domain/usecases/start_checkout.dart';
import 'package:totem/src/features/checkout/presentation/bloc/checkout_bloc.dart';

void main() {
  test('CheckoutBloc finaliza com sucesso após pagamento aprovado (cartão)', () async {
    final checkoutService = _CheckoutServiceApproved();
    final bloc = CheckoutBloc(
      startCheckout: StartCheckout(checkoutService),
      confirmPayment: ConfirmPayment(checkoutService),
    );
    addTearDown(bloc.close);

    bloc.add(
      const CheckoutStarted(
        items: <CheckoutItem>[
          CheckoutItem(
            id: 'line-1',
            title: 'Hambúrguer',
            skuCodes: <String>['X-BURGER'],
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

    expect(checkoutService.lastItems, isNotEmpty);
    expect(checkoutService.lastFulfillment, OrderFulfillment.dineIn);
    expect(checkoutService.lastPaymentMethod, PaymentMethod.creditCard);
  });

  test('CheckoutBloc não finaliza quando pagamento é negado', () async {
    final checkoutService = _CheckoutServiceDeclined();
    final bloc = CheckoutBloc(
      startCheckout: StartCheckout(checkoutService),
      confirmPayment: ConfirmPayment(checkoutService),
    );
    addTearDown(bloc.close);

    bloc.add(
      const CheckoutStarted(
        items: <CheckoutItem>[
          CheckoutItem(
            id: 'line-1',
            title: 'Hambúrguer',
            skuCodes: <String>['X-BURGER'],
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
  });

  test('CheckoutBloc ignora confirmação sem seleção completa', () async {
    final checkoutService = _CheckoutServiceApproved();
    final bloc = CheckoutBloc(
      startCheckout: StartCheckout(checkoutService),
      confirmPayment: ConfirmPayment(checkoutService),
    );
    addTearDown(bloc.close);

    bloc.add(
      const CheckoutStarted(
        items: <CheckoutItem>[
          CheckoutItem(
            id: 'line-1',
            title: 'Hambúrguer',
            skuCodes: <String>['X-BURGER'],
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
  });

  test('CheckoutBloc no Pix gera cobrança e só finaliza após confirmar pagamento', () async {
    final checkoutService = _CheckoutServiceApproved();
    final bloc = CheckoutBloc(
      startCheckout: StartCheckout(checkoutService),
      confirmPayment: ConfirmPayment(checkoutService),
    );
    addTearDown(bloc.close);

    bloc.add(
      const CheckoutStarted(
        items: <CheckoutItem>[
          CheckoutItem(
            id: 'line-1',
            title: 'Hambúrguer',
            skuCodes: <String>['X-BURGER'],
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

    bloc.add(const CheckoutPixPaymentConfirmed());
    await bloc.stream.firstWhere((s) => s.isSuccess);

    expect(checkoutService.confirmations, 1);
  });
}

class _CheckoutServiceApproved implements CheckoutService {
  int confirmations = 0;
  List<CheckoutItem> lastItems = <CheckoutItem>[];
  OrderFulfillment? lastFulfillment;
  PaymentMethod? lastPaymentMethod;

  @override
  Future<CheckoutStartResult> startCheckout({
    required List<CheckoutItem> items,
    required OrderFulfillment fulfillment,
    required PaymentMethod paymentMethod,
  }) async {
    lastItems = items;
    lastFulfillment = fulfillment;
    lastPaymentMethod = paymentMethod;
    return CheckoutStartResult(
      orderId: 'order-1',
      paymentId: 'payment-1',
      pixCharge: paymentMethod == PaymentMethod.pix
          ? PixCharge(
              amountCents: 2000,
              payload: 'payload-order-1',
              expiresAt: DateTime.now().add(const Duration(minutes: 5)),
              reference: 'order-1',
            )
          : null,
    );
  }

  @override
  Future<PaymentResult> confirmPayment({required String paymentId}) async {
    confirmations += 1;
    return const PaymentResult(isApproved: true, transactionId: 'tx-1', message: 'APROVADO');
  }
}

class _CheckoutServiceDeclined implements CheckoutService {
  @override
  Future<CheckoutStartResult> startCheckout({
    required List<CheckoutItem> items,
    required OrderFulfillment fulfillment,
    required PaymentMethod paymentMethod,
  }) async {
    return const CheckoutStartResult(orderId: 'order-1', paymentId: 'payment-1', pixCharge: null);
  }

  @override
  Future<PaymentResult> confirmPayment({required String paymentId}) async {
    return const PaymentResult(isApproved: false, message: 'NEGADO');
  }
}
