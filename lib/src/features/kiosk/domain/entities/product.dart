class Product {
  const Product({
    required this.id,
    required this.categoryId,
    required this.name,
    this.description,
    required this.baseSku,
    this.optionGroups = const <ProductOptionGroup>[],
  });

  final String id;
  final String categoryId;
  final String name;
  final String? description;
  final Sku baseSku;
  final List<ProductOptionGroup> optionGroups;

  int get priceCents => baseSku.priceCents;

  String? get imageUrl => baseSku.imageUrl;

  String get formattedPrice {
    final value = priceCents / 100;
    return 'R\$ ${value.toStringAsFixed(2).replaceAll('.', ',')}';
  }

  Map<String, Sku> get skuById {
    final all = <String, Sku>{baseSku.id: baseSku};
    for (final group in optionGroups) {
      for (final option in group.options) {
        all[option.sku.id] = option.sku;
      }
    }
    return all;
  }
}

class Sku {
  const Sku({
    required this.id,
    required this.name,
    required this.priceCents,
    this.imageUrl,
  });

  final String id;
  final String name;
  final int priceCents;
  final String? imageUrl;
}

class ProductOption {
  const ProductOption({
    required this.sku,
    this.isIncludedByDefault = false,
    this.isRemovable = true,
  });

  final Sku sku;
  final bool isIncludedByDefault;
  final bool isRemovable;
}

class ProductOptionGroup {
  const ProductOptionGroup({
    required this.id,
    required this.name,
    required this.options,
    this.allowMultiple = true,
    this.min = 0,
    this.max,
  });

  final String id;
  final String name;
  final List<ProductOption> options;
  final bool allowMultiple;
  final int min;
  final int? max;
}
