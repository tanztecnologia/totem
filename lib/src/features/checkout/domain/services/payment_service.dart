import '../entities/checkout_order.dart';
import '../entities/payment_result.dart';
import '../entities/pix_charge.dart';

abstract class PaymentService {
  Future<PixCharge> createPixCharge({
    required int amountCents,
    required String reference,
  });

  Future<PaymentResult> pay({
    required int amountCents,
    required PaymentMethod method,
  });
}
