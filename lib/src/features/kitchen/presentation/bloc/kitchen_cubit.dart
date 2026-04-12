import 'package:flutter_bloc/flutter_bloc.dart';

import '../../domain/entities/kitchen_order.dart';
import '../../domain/repositories/kitchen_repository.dart';

abstract class KitchenState {}

class KitchenInitial extends KitchenState {}

class KitchenLoading extends KitchenState {}

class KitchenLoaded extends KitchenState {
  final List<KitchenOrder> queued;
  final List<KitchenOrder> inPreparation;
  final List<KitchenOrder> ready;

  KitchenLoaded({
    required this.queued,
    required this.inPreparation,
    required this.ready,
  });
}

class KitchenError extends KitchenState {
  final String message;

  KitchenError(this.message);
}

class KitchenCubit extends Cubit<KitchenState> {
  final KitchenRepository _repository;

  KitchenCubit(this._repository) : super(KitchenInitial());

  Future<void> loadOrders() async {
    emit(KitchenLoading());
    try {
      final orders = await _repository.listOrders(statuses: [
        KitchenOrderStatus.queued,
        KitchenOrderStatus.inPreparation,
        KitchenOrderStatus.ready,
      ]);

      final queued = orders.where((o) => o.status == KitchenOrderStatus.queued).toList();
      final inPreparation = orders.where((o) => o.status == KitchenOrderStatus.inPreparation).toList();
      final ready = orders.where((o) => o.status == KitchenOrderStatus.ready).toList();

      emit(KitchenLoaded(
        queued: queued,
        inPreparation: inPreparation,
        ready: ready,
      ));
    } catch (e) {
      emit(KitchenError(e.toString()));
    }
  }

  Future<void> advanceOrderStatus(String orderId, KitchenOrderStatus currentStatus) async {
    try {
      KitchenOrderStatus nextStatus;
      switch (currentStatus) {
        case KitchenOrderStatus.queued:
          nextStatus = KitchenOrderStatus.inPreparation;
          break;
        case KitchenOrderStatus.inPreparation:
          nextStatus = KitchenOrderStatus.ready;
          break;
        case KitchenOrderStatus.ready:
          nextStatus = KitchenOrderStatus.completed;
          break;
        default:
          return;
      }

      await _repository.updateOrderStatus(orderId, nextStatus);
      await loadOrders();
    } catch (e) {
      emit(KitchenError(e.toString()));
    }
  }
}
