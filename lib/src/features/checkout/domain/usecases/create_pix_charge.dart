import '../entities/pix_charge.dart';
import '../services/payment_service.dart';

class CreatePixCharge {
  const CreatePixCharge(this._paymentService);

  final PaymentService _paymentService;

  Future<PixCharge> call({
    required int amountCents,
    required String reference,
  }) {
    return _paymentService.createPixCharge(amountCents: amountCents, reference: reference);
  }
}

