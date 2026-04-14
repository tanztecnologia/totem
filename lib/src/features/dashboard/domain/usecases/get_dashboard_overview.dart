import '../entities/dashboard_overview.dart';
import '../repositories/dashboard_repository.dart';

class GetDashboardOverview {
  const GetDashboardOverview(this._repository);

  final DashboardRepository _repository;

  Future<DashboardOverview> call({
    DateTime? fromInclusive,
    DateTime? toInclusive,
  }) {
    return _repository.getOverview(fromInclusive: fromInclusive, toInclusive: toInclusive);
  }
}
