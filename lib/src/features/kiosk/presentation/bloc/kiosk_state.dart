import '../../domain/entities/category.dart';
import '../../domain/entities/product.dart';

class KioskState {
  const KioskState({
    required this.isLoading,
    required this.categories,
    required this.selectedCategory,
    required this.products,
    required this.productById,
    required this.cartLinesById,
    required this.cartQtyByLineId,
  });

  factory KioskState.initial() {
    return const KioskState(
      isLoading: true,
      categories: <KioskCategory>[],
      selectedCategory: null,
      products: <Product>[],
      productById: <String, Product>{},
      cartLinesById: <String, CartLine>{},
      cartQtyByLineId: <String, int>{},
    );
  }

  final bool isLoading;
  final List<KioskCategory> categories;
  final KioskCategory? selectedCategory;
  final List<Product> products;
  final Map<String, Product> productById;
  final Map<String, CartLine> cartLinesById;
  final Map<String, int> cartQtyByLineId;

  int cartQtyFor(Product product) {
    var sum = 0;
    cartQtyByLineId.forEach((lineId, qty) {
      final line = cartLinesById[lineId];
      if (line == null) return;
      if (line.productId == product.id) sum += qty;
    });
    return sum;
  }

  int get cartItemsCount {
    var sum = 0;
    for (final qty in cartQtyByLineId.values) {
      sum += qty;
    }
    return sum;
  }

  int get cartTotalCents {
    var total = 0;
    cartQtyByLineId.forEach((lineId, qty) {
      final line = cartLinesById[lineId];
      if (line == null) return;
      final product = productById[line.productId];
      if (product == null) return;
      var extrasCents = 0;
      final skuById = product.skuById;
      for (final skuId in line.addedSkuIds) {
        extrasCents += skuById[skuId]?.priceCents ?? 0;
      }
      total += (product.priceCents + extrasCents) * qty;
    });
    return total;
  }

  String get cartTotalFormatted {
    final value = cartTotalCents / 100;
    return 'R\$ ${value.toStringAsFixed(2).replaceAll('.', ',')}';
  }

  KioskState copyWith({
    bool? isLoading,
    List<KioskCategory>? categories,
    KioskCategory? selectedCategory,
    List<Product>? products,
    Map<String, Product>? productById,
    Map<String, CartLine>? cartLinesById,
    Map<String, int>? cartQtyByLineId,
  }) {
    return KioskState(
      isLoading: isLoading ?? this.isLoading,
      categories: categories ?? this.categories,
      selectedCategory: selectedCategory ?? this.selectedCategory,
      products: products ?? this.products,
      productById: productById ?? this.productById,
      cartLinesById: cartLinesById ?? this.cartLinesById,
      cartQtyByLineId: cartQtyByLineId ?? this.cartQtyByLineId,
    );
  }
}

class CartLine {
  const CartLine({
    required this.id,
    required this.productId,
    required this.excludedSkuIds,
    required this.addedSkuIds,
  });

  final String id;
  final String productId;
  final List<String> excludedSkuIds;
  final List<String> addedSkuIds;

  static String buildId(
    String productId,
    List<String> excludedSkuIds,
    List<String> addedSkuIds,
  ) {
    final excluded = List<String>.from(excludedSkuIds)..sort();
    final added = List<String>.from(addedSkuIds)..sort();
    if (excluded.isEmpty && added.isEmpty) return productId;
    return '$productId::-(${excluded.join(',')})::+(${added.join(',')})';
  }
}
