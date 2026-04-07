import 'package:bloc/bloc.dart';

import '../../domain/entities/checkout_order.dart';
import '../../domain/usecases/create_pix_charge.dart';
import '../../domain/usecases/process_payment.dart';
import '../../domain/usecases/place_order.dart';
import 'checkout_event.dart';
import 'checkout_state.dart';

export 'checkout_event.dart';
export 'checkout_state.dart';

class CheckoutBloc extends Bloc<CheckoutEvent, CheckoutState> {
  CheckoutBloc({
    required PlaceOrder placeOrder,
    required CreatePixCharge createPixCharge,
    required ProcessPayment processPayment,
  })  : _placeOrder = placeOrder,
        _createPixCharge = createPixCharge,
        _processPayment = processPayment,
        super(CheckoutState.initial()) {
    on<CheckoutStarted>(_onStarted);
    on<CheckoutFulfillmentSelected>(_onFulfillmentSelected);
    on<CheckoutPaymentMethodSelected>(_onPaymentSelected);
    on<CheckoutBackRequested>(_onBackRequested);
    on<CheckoutConfirmed>(_onConfirmed);
    on<CheckoutPixPaymentConfirmed>(_onPixPaymentConfirmed);
  }

  final PlaceOrder _placeOrder;
  final CreatePixCharge _createPixCharge;
  final ProcessPayment _processPayment;

  void _onStarted(
    CheckoutStarted event,
    Emitter<CheckoutState> emit,
  ) {
    emit(
      state.copyWith(
        items: event.items,
        totalCents: event.totalCents,
        totalText: event.totalText,
        step: CheckoutStep.fulfillment,
        fulfillment: null,
        paymentMethod: null,
        pixCharge: null,
        paymentTransactionId: null,
        isSubmitting: false,
        isSuccess: false,
        orderId: null,
        errorMessage: null,
      ),
    );
  }

  void _onFulfillmentSelected(
    CheckoutFulfillmentSelected event,
    Emitter<CheckoutState> emit,
  ) {
    emit(state.copyWith(step: CheckoutStep.payment, fulfillment: event.fulfillment, errorMessage: null));
  }

  void _onPaymentSelected(
    CheckoutPaymentMethodSelected event,
    Emitter<CheckoutState> emit,
  ) {
    emit(state.copyWith(paymentMethod: event.paymentMethod, errorMessage: null));
  }

  void _onBackRequested(
    CheckoutBackRequested event,
    Emitter<CheckoutState> emit,
  ) {
    if (state.isSubmitting) return;
    if (state.step == CheckoutStep.pixQr) {
      emit(state.copyWith(step: CheckoutStep.payment, pixCharge: null, paymentTransactionId: null, errorMessage: null));
    } else if (state.step == CheckoutStep.cardPrompt) {
      emit(state.copyWith(step: CheckoutStep.payment, paymentTransactionId: null, errorMessage: null));
    } else if (state.step == CheckoutStep.payment) {
      emit(
        state.copyWith(
          step: CheckoutStep.fulfillment,
          paymentMethod: null,
          pixCharge: null,
          paymentTransactionId: null,
          errorMessage: null,
        ),
      );
    }
  }

  Future<void> _onConfirmed(
    CheckoutConfirmed event,
    Emitter<CheckoutState> emit,
  ) async {
    if (state.isSubmitting) return;
    if (state.items.isEmpty) return;
    final fulfillment = state.fulfillment;
    final paymentMethod = state.paymentMethod;
    if (fulfillment == null || paymentMethod == null) return;
    if (state.step != CheckoutStep.payment) return;

    if (paymentMethod == PaymentMethod.pix) {
      emit(state.copyWith(isSubmitting: true, errorMessage: null));
      final charge = await _createPixCharge(
        amountCents: state.totalCents,
        reference: 'order-${DateTime.now().millisecondsSinceEpoch}',
      );
      emit(state.copyWith(isSubmitting: false, step: CheckoutStep.pixQr, pixCharge: charge));
      return;
    }

    emit(state.copyWith(isSubmitting: true, step: CheckoutStep.cardPrompt, errorMessage: null));

    final payment = await _processPayment(amountCents: state.totalCents, method: paymentMethod);
    if (!payment.isApproved) {
      emit(
        state.copyWith(
          step: CheckoutStep.payment,
          isSubmitting: false,
          paymentTransactionId: payment.transactionId,
          errorMessage: payment.message ?? 'Pagamento não aprovado',
        ),
      );
      return;
    }

    final orderId = await _placeOrder(
      CheckoutOrder(
        items: state.items,
        totalCents: state.totalCents,
        fulfillment: fulfillment,
        paymentMethod: paymentMethod,
      ),
    );
    emit(
      state.copyWith(
        isSubmitting: false,
        isSuccess: true,
        orderId: orderId,
        paymentTransactionId: payment.transactionId,
        step: CheckoutStep.success,
      ),
    );
  }

  Future<void> _onPixPaymentConfirmed(
    CheckoutPixPaymentConfirmed event,
    Emitter<CheckoutState> emit,
  ) async {
    if (state.isSubmitting) return;
    if (state.items.isEmpty) return;
    final fulfillment = state.fulfillment;
    final paymentMethod = state.paymentMethod;
    if (fulfillment == null || paymentMethod != PaymentMethod.pix) return;
    if (state.step != CheckoutStep.pixQr) return;

    emit(state.copyWith(isSubmitting: true, errorMessage: null));

    final payment = await _processPayment(amountCents: state.totalCents, method: PaymentMethod.pix);
    if (!payment.isApproved) {
      emit(
        state.copyWith(
          isSubmitting: false,
          paymentTransactionId: payment.transactionId,
          errorMessage: payment.message ?? 'Pagamento não aprovado',
        ),
      );
      return;
    }

    final orderId = await _placeOrder(
      CheckoutOrder(
        items: state.items,
        totalCents: state.totalCents,
        fulfillment: fulfillment,
        paymentMethod: PaymentMethod.pix,
      ),
    );
    emit(
      state.copyWith(
        isSubmitting: false,
        isSuccess: true,
        orderId: orderId,
        paymentTransactionId: payment.transactionId,
        step: CheckoutStep.success,
      ),
    );
  }
}
