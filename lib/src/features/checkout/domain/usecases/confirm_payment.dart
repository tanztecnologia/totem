import '../entities/payment_result.dart';
import '../services/checkout_service.dart';

class ConfirmPayment {
  const ConfirmPayment(this._service);

  final CheckoutService _service;

  Future<PaymentResult> call({
    required String paymentId,
  }) {
    return _service.confirmPayment(paymentId: paymentId);
  }
}
