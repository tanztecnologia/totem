import '../../domain/entities/category.dart';
import '../../domain/entities/product.dart';

sealed class KioskEvent {
  const KioskEvent();
}

final class KioskLoadRequested extends KioskEvent {
  const KioskLoadRequested();
}

final class KioskCategorySelected extends KioskEvent {
  const KioskCategorySelected(this.category);

  final KioskCategory category;
}

final class KioskProductAdded extends KioskEvent {
  const KioskProductAdded(
    this.product, {
    this.quantity = 1,
    this.excludedSkuIds = const <String>[],
    this.addedSkuIds = const <String>[],
  });

  final Product product;
  final int quantity;
  final List<String> excludedSkuIds;
  final List<String> addedSkuIds;
}

final class KioskCartLineDecremented extends KioskEvent {
  const KioskCartLineDecremented(this.lineId, {this.quantity = 1});

  final String lineId;
  final int quantity;
}

final class KioskCartCleared extends KioskEvent {
  const KioskCartCleared();
}
