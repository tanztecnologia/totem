import 'package:flutter_test/flutter_test.dart';
import 'package:totem/src/features/kiosk/data/repositories/api_catalog_repository.dart';

void main() {
  test('mapSkusToCategories cria categorias ordenadas', () {
    final skus = <ApiSkuDto>[
      ApiSkuDto(id: '1', categoryCode: '00002', code: '00010', name: 'X Burger', priceCents: 2500, imageUrl: null, isActive: true),
      ApiSkuDto(id: '2', categoryCode: '00001', code: '00011', name: 'Coca 350', priceCents: 800, imageUrl: null, isActive: true),
    ];

    final categories = <ApiCategoryDto>[
      ApiCategoryDto(code: '00002', slug: 'x', name: 'X Burger Cat'),
      ApiCategoryDto(code: '00001', slug: 'coca', name: 'Coca Cat'),
    ];

    final result = mapSkusToCategories(skus, categories);
    expect(result.map((c) => c.id).toList(), ['00001', '00002']);
  });

  test('mapSkusToProductsForCategory usa o id do SKU (compatível com checkout)', () {
    final skus = <ApiSkuDto>[
      ApiSkuDto(id: '1', categoryCode: '00002', code: '00010', name: 'X Burger', priceCents: 2500, imageUrl: 'http://img', isActive: true),
    ];

    final products = mapSkusToProductsForCategory(skus, '00002');
    expect(products, hasLength(1));

    final product = products.first;
    expect(product.id, '1');
    expect(product.categoryId, '00002');
    expect(product.name, 'X Burger');
    expect(product.baseSku.id, '1');
    expect(product.baseSku.priceCents, 2500);
    expect(product.baseSku.imageUrl, 'http://img');
  });
}
