import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../checkout/domain/entities/checkout_order.dart';
import '../../domain/entities/pdv_order.dart';
import '../../domain/repositories/pdv_repository.dart';
import '../../domain/usecases/list_pdv_orders_by_comanda.dart';
import '../../domain/usecases/pay_pdv_order.dart';
import 'pdv_state.dart';

class PdvCubit extends Cubit<PdvState> {
  PdvCubit({
    required PdvRepository repository,
    required ListPdvOrdersByComanda listOrdersByComanda,
    required PayPdvOrder payOrder,
  })  : _repository = repository,
        _listOrdersByComanda = listOrdersByComanda,
        _payOrder = payOrder,
        super(PdvState.initial());

  final PdvRepository _repository;
  final ListPdvOrdersByComanda _listOrdersByComanda;
  final PayPdvOrder _payOrder;

  Future<void> loadCashRegister() async {
    if (state.isCashRegisterLoading) return;
    emit(state.copyWith(isCashRegisterLoading: true, errorMessage: null));
    try {
      final shift = await _repository.getCurrentCashRegisterShift();
      emit(state.copyWith(isCashRegisterLoading: false, cashRegisterShift: shift));
    } catch (e) {
      emit(state.copyWith(isCashRegisterLoading: false, errorMessage: e.toString()));
    }
  }

  Future<void> openCashRegister({required int openingCashCents}) async {
    if (state.isCashRegisterBusy) return;
    emit(state.copyWith(isCashRegisterBusy: true, errorMessage: null));
    try {
      final opened = await _repository.openCashRegisterShift(openingCashCents: openingCashCents);
      emit(
        state.copyWith(
          isCashRegisterBusy: false,
          cashRegisterShift: opened,
          lastCashRegisterOpenedShift: opened,
        ),
      );
    } catch (e) {
      emit(state.copyWith(isCashRegisterBusy: false, errorMessage: e.toString()));
    }
  }

  Future<void> closeCashRegister({required int closingCashCents}) async {
    if (state.isCashRegisterBusy) return;
    emit(state.copyWith(isCashRegisterBusy: true, errorMessage: null));
    try {
      final result = await _repository.closeCashRegisterShift(closingCashCents: closingCashCents);
      emit(
        state.copyWith(
          isCashRegisterBusy: false,
          cashRegisterShift: null,
          lastCashRegisterClosedResult: result,
        ),
      );
    } catch (e) {
      emit(state.copyWith(isCashRegisterBusy: false, errorMessage: e.toString()));
    }
  }

  void clearCashRegisterNotifications() {
    emit(
      state.copyWith(
        lastCashRegisterOpenedShift: null,
        lastCashRegisterClosedResult: null,
      ),
    );
  }

  void setComanda(String value) {
    emit(state.copyWith(comanda: value, errorMessage: null, selectedOrderId: null));
  }

  void setIncludePaid(bool value) {
    emit(state.copyWith(includePaid: value, errorMessage: null));
  }

  void selectOrder(String? orderId) {
    emit(state.copyWith(selectedOrderId: orderId, transactionId: '', errorMessage: null));
  }

  void setPaymentMethod(PaymentMethod? method) {
    emit(state.copyWith(paymentMethod: method, transactionId: '', errorMessage: null));
  }

  void setTransactionId(String value) {
    emit(state.copyWith(transactionId: value, errorMessage: null));
  }

  Future<void> search() async {
    final comanda = state.comanda.trim();
    if (comanda.isEmpty) {
      emit(state.copyWith(errorMessage: 'Informe a comanda.'));
      return;
    }

    emit(state.copyWith(isLoading: true, errorMessage: null, lastPayment: null));
    try {
      final orders = await _listOrdersByComanda(
        comanda: comanda,
        includePaid: state.includePaid,
      );
      final keepSelected =
          state.selectedOrderId != null && orders.any((o) => o.orderId == state.selectedOrderId);
      String? nextSelected;
      if (keepSelected) {
        nextSelected = state.selectedOrderId;
      } else {
        final unpaid = orders.where((o) => !o.isPaid).toList(growable: false);
        if (unpaid.isNotEmpty) {
          nextSelected = unpaid.first.orderId;
        } else if (orders.isNotEmpty) {
          nextSelected = orders.first.orderId;
        } else {
          nextSelected = null;
        }
      }

      emit(state.copyWith(isLoading: false, orders: orders, selectedOrderId: nextSelected));
    } catch (e) {
      emit(state.copyWith(isLoading: false, errorMessage: e.toString(), orders: const <PdvOrder>[]));
    }
  }

  Future<void> pay({
    required String orderId,
    required PaymentMethod method,
    String? transactionId,
  }) async {
    if (state.isPaying) return;
    if (state.cashRegisterShift == null && !state.isCashRegisterLoading) {
      await loadCashRegister();
    }
    if (state.cashRegisterShift?.isOpen != true) {
      emit(state.copyWith(errorMessage: 'Caixa fechado. Abra o caixa para receber pagamentos.'));
      return;
    }
    emit(state.copyWith(isPaying: true, payingOrderId: orderId, errorMessage: null));
    try {
      final result = await _payOrder(
        orderId: orderId,
        paymentMethod: method,
        transactionId: transactionId,
      );
      emit(state.copyWith(isPaying: false, payingOrderId: null, lastPayment: result));
      await search();
    } catch (e) {
      emit(state.copyWith(isPaying: false, payingOrderId: null, errorMessage: e.toString()));
    }
  }
}
