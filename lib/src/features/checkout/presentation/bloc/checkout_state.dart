import '../../domain/entities/checkout_item.dart';
import '../../domain/entities/checkout_order.dart';
import '../../domain/entities/pix_charge.dart';

enum CheckoutStep {
  fulfillment,
  payment,
  pixQr,
  cardPrompt,
  success,
}

class CheckoutState {
  const CheckoutState({
    required this.items,
    required this.totalCents,
    required this.totalText,
    required this.step,
    required this.fulfillment,
    required this.paymentMethod,
    required this.pixCharge,
    required this.paymentTransactionId,
    required this.isSubmitting,
    required this.isSuccess,
    required this.orderId,
    required this.errorMessage,
  });

  factory CheckoutState.initial() {
    return const CheckoutState(
      items: <CheckoutItem>[],
      totalCents: 0,
      totalText: '',
      step: CheckoutStep.fulfillment,
      fulfillment: null,
      paymentMethod: null,
      pixCharge: null,
      paymentTransactionId: null,
      isSubmitting: false,
      isSuccess: false,
      orderId: null,
      errorMessage: null,
    );
  }

  final List<CheckoutItem> items;
  final int totalCents;
  final String totalText;
  final CheckoutStep step;
  final OrderFulfillment? fulfillment;
  final PaymentMethod? paymentMethod;
  final PixCharge? pixCharge;
  final String? paymentTransactionId;
  final bool isSubmitting;
  final bool isSuccess;
  final String? orderId;
  final String? errorMessage;

  static const Object _unset = Object();

  CheckoutState copyWith({
    List<CheckoutItem>? items,
    int? totalCents,
    String? totalText,
    CheckoutStep? step,
    OrderFulfillment? fulfillment,
    PaymentMethod? paymentMethod,
    PixCharge? pixCharge,
    String? paymentTransactionId,
    bool? isSubmitting,
    bool? isSuccess,
    String? orderId,
    Object? errorMessage = _unset,
  }) {
    return CheckoutState(
      items: items ?? this.items,
      totalCents: totalCents ?? this.totalCents,
      totalText: totalText ?? this.totalText,
      step: step ?? this.step,
      fulfillment: fulfillment ?? this.fulfillment,
      paymentMethod: paymentMethod ?? this.paymentMethod,
      pixCharge: pixCharge ?? this.pixCharge,
      paymentTransactionId: paymentTransactionId ?? this.paymentTransactionId,
      isSubmitting: isSubmitting ?? this.isSubmitting,
      isSuccess: isSuccess ?? this.isSuccess,
      orderId: orderId ?? this.orderId,
      errorMessage: identical(errorMessage, _unset) ? this.errorMessage : errorMessage as String?,
    );
  }
}
