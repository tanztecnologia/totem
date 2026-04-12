import 'package:flutter_test/flutter_test.dart';
import 'package:totem/src/features/kiosk/data/repositories/api_catalog_repository.dart';

void main() {
  test('categoryIdFromSkuCode deriva categoria do code', () {
    expect(categoryIdFromSkuCode('X-BURGER'), 'x');
    expect(categoryIdFromSkuCode('COCA_350'), 'coca');
    expect(categoryIdFromSkuCode('  '), 'outros');
  });

  test('displayNameFromCategoryId gera um nome legível', () {
    expect(displayNameFromCategoryId('x'), 'X');
    expect(displayNameFromCategoryId('coca'), 'Coca');
    expect(displayNameFromCategoryId(''), 'Outros');
  });

  test('mapSkusToCategories cria categorias ordenadas', () {
    final skus = <ApiSkuDto>[
      ApiSkuDto(id: '1', code: 'X-BURGER', name: 'X Burger', priceCents: 2500, imageUrl: null, isActive: true),
      ApiSkuDto(id: '2', code: 'COCA-350', name: 'Coca 350', priceCents: 800, imageUrl: null, isActive: true),
    ];

    final categories = mapSkusToCategories(skus);
    expect(categories.map((c) => c.id).toList(), ['coca', 'x']);
    expect(categories.map((c) => c.name).toList(), ['Coca', 'X']);
  });

  test('mapSkusToProductsForCategory usa o code como id do SKU (compatível com checkout)', () {
    final skus = <ApiSkuDto>[
      ApiSkuDto(id: '1', code: 'X-BURGER', name: 'X Burger', priceCents: 2500, imageUrl: 'http://img', isActive: true),
    ];

    final products = mapSkusToProductsForCategory(skus, 'x');
    expect(products, hasLength(1));
    expect(products.first.id, 'X-BURGER');
    expect(products.first.baseSku.id, 'X-BURGER');
    expect(products.first.baseSku.priceCents, 2500);
  });
}
