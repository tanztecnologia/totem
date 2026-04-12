import 'package:bloc/bloc.dart';

import '../../domain/entities/checkout_order.dart';
import '../../domain/usecases/confirm_payment.dart';
import '../../domain/usecases/start_checkout.dart';
import 'checkout_event.dart';
import 'checkout_state.dart';

export 'checkout_event.dart';
export 'checkout_state.dart';

class CheckoutBloc extends Bloc<CheckoutEvent, CheckoutState> {
  CheckoutBloc({
    required StartCheckout startCheckout,
    required ConfirmPayment confirmPayment,
  })  : _startCheckout = startCheckout,
        _confirmPayment = confirmPayment,
        super(CheckoutState.initial()) {
    on<CheckoutStarted>(_onStarted);
    on<CheckoutFulfillmentSelected>(_onFulfillmentSelected);
    on<CheckoutPaymentMethodSelected>(_onPaymentSelected);
    on<CheckoutBackRequested>(_onBackRequested);
    on<CheckoutConfirmed>(_onConfirmed);
    on<CheckoutPixPaymentConfirmed>(_onPixPaymentConfirmed);
  }

  final StartCheckout _startCheckout;
  final ConfirmPayment _confirmPayment;

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
        paymentId: null,
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
      emit(
        state.copyWith(
          step: CheckoutStep.payment,
          pixCharge: null,
          paymentTransactionId: null,
          paymentId: null,
          orderId: null,
          errorMessage: null,
        ),
      );
    } else if (state.step == CheckoutStep.cardPrompt) {
      emit(
        state.copyWith(
          step: CheckoutStep.payment,
          paymentTransactionId: null,
          paymentId: null,
          orderId: null,
          errorMessage: null,
        ),
      );
    } else if (state.step == CheckoutStep.payment) {
      emit(
        state.copyWith(
          step: CheckoutStep.fulfillment,
          paymentMethod: null,
          pixCharge: null,
          paymentTransactionId: null,
          paymentId: null,
          orderId: null,
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
      try {
        final started = await _startCheckout(
          items: state.items,
          fulfillment: fulfillment,
          paymentMethod: PaymentMethod.pix,
        );
        emit(
          state.copyWith(
            isSubmitting: false,
            step: CheckoutStep.pixQr,
            pixCharge: started.pixCharge,
            paymentId: started.paymentId,
            orderId: started.orderId,
          ),
        );
      } catch (e) {
        emit(state.copyWith(isSubmitting: false, errorMessage: e.toString()));
      }
      return;
    }

    emit(state.copyWith(isSubmitting: true, step: CheckoutStep.cardPrompt, errorMessage: null));

    try {
      final started = await _startCheckout(
        items: state.items,
        fulfillment: fulfillment,
        paymentMethod: paymentMethod,
      );

      final payment = await _confirmPayment(paymentId: started.paymentId);
      if (!payment.isApproved) {
        emit(
          state.copyWith(
            step: CheckoutStep.payment,
            isSubmitting: false,
            paymentId: null,
            orderId: null,
            paymentTransactionId: payment.transactionId,
            errorMessage: payment.message ?? 'Pagamento não aprovado',
          ),
        );
        return;
      }

      emit(
        state.copyWith(
          isSubmitting: false,
          isSuccess: true,
          paymentId: started.paymentId,
          orderId: started.orderId,
          paymentTransactionId: payment.transactionId,
          step: CheckoutStep.success,
        ),
      );
    } catch (e) {
      emit(
        state.copyWith(
          step: CheckoutStep.payment,
          isSubmitting: false,
          paymentId: null,
          orderId: null,
          errorMessage: e.toString(),
        ),
      );
    }
  }

  Future<void> _onPixPaymentConfirmed(
    CheckoutPixPaymentConfirmed event,
    Emitter<CheckoutState> emit,
  ) async {
    if (state.isSubmitting) return;
    if (state.items.isEmpty) return;
    if (state.paymentMethod != PaymentMethod.pix) return;
    if (state.step != CheckoutStep.pixQr) return;
    final paymentId = state.paymentId;
    final orderId = state.orderId;
    if (paymentId == null || orderId == null) return;

    emit(state.copyWith(isSubmitting: true, errorMessage: null));

    try {
      final payment = await _confirmPayment(paymentId: paymentId);
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

      emit(
        state.copyWith(
          isSubmitting: false,
          isSuccess: true,
          orderId: orderId,
          paymentTransactionId: payment.transactionId,
          step: CheckoutStep.success,
        ),
      );
    } catch (e) {
      emit(state.copyWith(isSubmitting: false, errorMessage: e.toString()));
    }
  }
}
