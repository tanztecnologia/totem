import '../entities/checkout_order.dart';
import '../entities/payment_result.dart';
import '../services/payment_service.dart';

class ProcessPayment {
  const ProcessPayment(this._service);

  final PaymentService _service;

  Future<PaymentResult> call({
    required int amountCents,
    required PaymentMethod method,
  }) {
    return _service.pay(amountCents: amountCents, method: method);
  }
}

