import '../../../checkout/domain/entities/checkout_order.dart';
import '../../domain/entities/pdv_order.dart';
import '../../domain/repositories/pdv_repository.dart';

class PdvState {
  const PdvState({
    required this.comanda,
    required this.includePaid,
    required this.orders,
    required this.selectedOrderId,
    required this.cashRegisterShift,
    required this.isCashRegisterLoading,
    required this.isCashRegisterBusy,
    required this.paymentMethod,
    required this.transactionId,
    required this.isLoading,
    required this.isPaying,
    required this.payingOrderId,
    required this.lastPayment,
    required this.lastCashRegisterOpenedShift,
    required this.lastCashRegisterClosedResult,
    required this.errorMessage,
  });

  factory PdvState.initial() => const PdvState(
        comanda: '',
        includePaid: false,
        orders: <PdvOrder>[],
        selectedOrderId: null,
        cashRegisterShift: null,
        isCashRegisterLoading: false,
        isCashRegisterBusy: false,
        paymentMethod: null,
        transactionId: '',
        isLoading: false,
        isPaying: false,
        payingOrderId: null,
        lastPayment: null,
        lastCashRegisterOpenedShift: null,
        lastCashRegisterClosedResult: null,
        errorMessage: null,
      );

  final String comanda;
  final bool includePaid;
  final List<PdvOrder> orders;
  final String? selectedOrderId;
  final PdvCashRegisterShift? cashRegisterShift;
  final bool isCashRegisterLoading;
  final bool isCashRegisterBusy;
  final PaymentMethod? paymentMethod;
  final String transactionId;
  final bool isLoading;
  final bool isPaying;
  final String? payingOrderId;
  final PdvPaymentResult? lastPayment;
  final PdvCashRegisterShift? lastCashRegisterOpenedShift;
  final PdvCloseCashRegisterResult? lastCashRegisterClosedResult;
  final String? errorMessage;

  static const Object _unset = Object();

  PdvState copyWith({
    String? comanda,
    bool? includePaid,
    List<PdvOrder>? orders,
    Object? selectedOrderId = _unset,
    Object? cashRegisterShift = _unset,
    bool? isCashRegisterLoading,
    bool? isCashRegisterBusy,
    Object? paymentMethod = _unset,
    String? transactionId,
    bool? isLoading,
    bool? isPaying,
    Object? payingOrderId = _unset,
    Object? lastPayment = _unset,
    Object? lastCashRegisterOpenedShift = _unset,
    Object? lastCashRegisterClosedResult = _unset,
    String? errorMessage,
  }) {
    return PdvState(
      comanda: comanda ?? this.comanda,
      includePaid: includePaid ?? this.includePaid,
      orders: orders ?? this.orders,
      selectedOrderId: identical(selectedOrderId, _unset) ? this.selectedOrderId : selectedOrderId as String?,
      cashRegisterShift: identical(cashRegisterShift, _unset) ? this.cashRegisterShift : cashRegisterShift as PdvCashRegisterShift?,
      isCashRegisterLoading: isCashRegisterLoading ?? this.isCashRegisterLoading,
      isCashRegisterBusy: isCashRegisterBusy ?? this.isCashRegisterBusy,
      paymentMethod: identical(paymentMethod, _unset) ? this.paymentMethod : paymentMethod as PaymentMethod?,
      transactionId: transactionId ?? this.transactionId,
      isLoading: isLoading ?? this.isLoading,
      isPaying: isPaying ?? this.isPaying,
      payingOrderId: identical(payingOrderId, _unset) ? this.payingOrderId : payingOrderId as String?,
      lastPayment: identical(lastPayment, _unset) ? this.lastPayment : lastPayment as PdvPaymentResult?,
      lastCashRegisterOpenedShift: identical(lastCashRegisterOpenedShift, _unset)
          ? this.lastCashRegisterOpenedShift
          : lastCashRegisterOpenedShift as PdvCashRegisterShift?,
      lastCashRegisterClosedResult: identical(lastCashRegisterClosedResult, _unset)
          ? this.lastCashRegisterClosedResult
          : lastCashRegisterClosedResult as PdvCloseCashRegisterResult?,
      errorMessage: errorMessage,
    );
  }
}
