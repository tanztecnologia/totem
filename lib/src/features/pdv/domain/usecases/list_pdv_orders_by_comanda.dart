import '../entities/pdv_order.dart';
import '../repositories/pdv_repository.dart';

class ListPdvOrdersByComanda {
  const ListPdvOrdersByComanda(this._repository);

  final PdvRepository _repository;

  Future<List<PdvOrder>> call({
    required String comanda,
    required bool includePaid,
    int limit = 50,
  }) {
    return _repository.listOrdersByComanda(comanda: comanda, includePaid: includePaid, limit: limit);
  }
}

