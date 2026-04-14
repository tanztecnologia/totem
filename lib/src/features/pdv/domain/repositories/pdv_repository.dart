import '../entities/pdv_order.dart';
import '../../../checkout/domain/entities/checkout_order.dart';

abstract class PdvRepository {
  Future<List<PdvOrder>> listOrdersByComanda({
    required String comanda,
    required bool includePaid,
    int limit = 50,
  });

  Future<PdvCashRegisterShift?> getCurrentCashRegisterShift();

  Future<PdvCashRegisterShift> openCashRegisterShift({
    required int openingCashCents,
  });

  Future<PdvCloseCashRegisterResult> closeCashRegisterShift({
    required int closingCashCents,
  });

  Future<PdvPaymentResult> payOrder({
    required String orderId,
    required PaymentMethod paymentMethod,
    String? transactionId,
  });
}

enum PdvCashRegisterShiftStatus {
  open,
  closed,
}

class PdvCashRegisterShift {
  const PdvCashRegisterShift({
    required this.id,
    required this.status,
    required this.openedByEmail,
    required this.openingCashCents,
    required this.openedAt,
    required this.closingCashCents,
    required this.totalSalesCents,
    required this.totalCashSalesCents,
    required this.expectedCashCents,
    required this.closedAt,
  });

  final String id;
  final PdvCashRegisterShiftStatus status;
  final String openedByEmail;
  final int openingCashCents;
  final DateTime openedAt;
  final int? closingCashCents;
  final int? totalSalesCents;
  final int? totalCashSalesCents;
  final int? expectedCashCents;
  final DateTime? closedAt;

  bool get isOpen => status == PdvCashRegisterShiftStatus.open;
}

class PdvCloseCashRegisterResult {
  const PdvCloseCashRegisterResult({
    required this.shift,
    required this.totalSalesCents,
    required this.totalCashSalesCents,
    required this.expectedCashCents,
    required this.closingCashCents,
    required this.differenceCents,
    required this.payments,
  });

  final PdvCashRegisterShift shift;
  final int totalSalesCents;
  final int totalCashSalesCents;
  final int expectedCashCents;
  final int closingCashCents;
  final int differenceCents;
  final List<PdvPaymentMethodSummaryItem> payments;
}

class PdvPaymentMethodSummaryItem {
  const PdvPaymentMethodSummaryItem({
    required this.method,
    required this.amountCents,
  });

  final PaymentMethod method;
  final int amountCents;
}

class PdvPaymentResult {
  const PdvPaymentResult({
    required this.orderId,
    required this.orderStatus,
    required this.kitchenStatus,
    required this.paymentStatus,
    required this.transactionId,
  });

  final String orderId;
  final String orderStatus;
  final String kitchenStatus;
  final String paymentStatus;
  final String transactionId;

  bool get isApproved => paymentStatus == 'Approved';
}
