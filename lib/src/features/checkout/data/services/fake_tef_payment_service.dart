import '../../domain/entities/checkout_order.dart';
import '../../domain/entities/payment_result.dart';
import '../../domain/entities/pix_charge.dart';
import '../../domain/services/payment_service.dart';

class FakeTefPaymentService implements PaymentService {
  const FakeTefPaymentService();

  @override
  Future<PixCharge> createPixCharge({
    required int amountCents,
    required String reference,
  }) async {
    await Future<void>.delayed(const Duration(milliseconds: 400));
    final now = DateTime.now();
    final expiresAt = now.add(const Duration(minutes: 5));
    final payload = '000201|AMOUNT=$amountCents|REF=$reference|EXPIRES=${expiresAt.toIso8601String()}';
    return PixCharge(
      amountCents: amountCents,
      payload: payload,
      expiresAt: expiresAt,
      reference: reference,
    );
  }

  @override
  Future<PaymentResult> pay({
    required int amountCents,
    required PaymentMethod method,
  }) async {
    final delay = switch (method) {
      PaymentMethod.creditCard || PaymentMethod.debitCard => const Duration(seconds: 30),
      PaymentMethod.pix => const Duration(milliseconds: 700),
      PaymentMethod.cash => const Duration(milliseconds: 200),
    };
    await Future<void>.delayed(delay);
    return PaymentResult(
      isApproved: true,
      transactionId: DateTime.now().millisecondsSinceEpoch.toString(),
      message: 'APROVADO',
    );
  }
}
