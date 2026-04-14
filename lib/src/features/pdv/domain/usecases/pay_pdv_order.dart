import '../../../checkout/domain/entities/checkout_order.dart';
import '../repositories/pdv_repository.dart';

class PayPdvOrder {
  const PayPdvOrder(this._repository);

  final PdvRepository _repository;

  Future<PdvPaymentResult> call({
    required String orderId,
    required PaymentMethod paymentMethod,
    String? transactionId,
  }) {
    return _repository.payOrder(
      orderId: orderId,
      paymentMethod: paymentMethod,
      transactionId: transactionId,
    );
  }
}

