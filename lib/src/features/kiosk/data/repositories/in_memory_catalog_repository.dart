import '../../domain/entities/category.dart';
import '../../domain/entities/product.dart';
import '../../domain/repositories/catalog_repository.dart';

class InMemoryCatalogRepository implements CatalogRepository {
  const InMemoryCatalogRepository();

  static const List<KioskCategory> _categories = <KioskCategory>[
    KioskCategory(id: 'drinks', name: 'Bebidas'),
    KioskCategory(id: 'snacks', name: 'Lanches'),
    KioskCategory(id: 'meals', name: 'Refeições'),
    KioskCategory(id: 'desserts', name: 'Sobremesas'),
  ];

  static const List<Product> _products = <Product>[
    Product(
      id: 'coke',
      categoryId: 'drinks',
      name: 'Refrigerante',
      baseSku: Sku(
        id: 'coke',
        name: 'Refrigerante',
        priceCents: 690,
        imageUrl: 'https://picsum.photos/seed/coke/600/450',
      ),
    ),
    Product(
      id: 'water',
      categoryId: 'drinks',
      name: 'Água',
      baseSku: Sku(
        id: 'water',
        name: 'Água',
        priceCents: 350,
        imageUrl: 'https://picsum.photos/seed/water/600/450',
      ),
    ),
    Product(
      id: 'juice',
      categoryId: 'drinks',
      name: 'Suco',
      baseSku: Sku(
        id: 'juice',
        name: 'Suco',
        priceCents: 790,
        imageUrl: 'https://picsum.photos/seed/juice/600/450',
      ),
    ),
    Product(
      id: 'coffee',
      categoryId: 'drinks',
      name: 'Café',
      baseSku: Sku(
        id: 'coffee',
        name: 'Café',
        priceCents: 450,
        imageUrl: 'https://picsum.photos/seed/coffee/600/450',
      ),
    ),
    Product(
      id: 'burger',
      categoryId: 'snacks',
      name: 'Hambúrguer',
      baseSku: Sku(
        id: 'burger',
        name: 'Hambúrguer',
        priceCents: 1990,
        imageUrl: 'https://picsum.photos/seed/burger/600/450',
      ),
      optionGroups: <ProductOptionGroup>[
        ProductOptionGroup(
          id: 'ingredients',
          name: 'Ingredientes',
          options: <ProductOption>[
            ProductOption(
              sku: Sku(id: 'bun', name: 'Pão', priceCents: 0),
              isIncludedByDefault: true,
              isRemovable: false,
            ),
            ProductOption(
              sku: Sku(id: 'patty', name: 'Hambúrguer', priceCents: 0),
              isIncludedByDefault: true,
              isRemovable: false,
            ),
            ProductOption(
              sku: Sku(id: 'lettuce', name: 'Alface', priceCents: 0),
              isIncludedByDefault: true,
            ),
            ProductOption(
              sku: Sku(id: 'tomato', name: 'Tomate', priceCents: 0),
              isIncludedByDefault: true,
            ),
            ProductOption(
              sku: Sku(id: 'cheese', name: 'Queijo', priceCents: 0),
              isIncludedByDefault: true,
            ),
            ProductOption(
              sku: Sku(id: 'mayo', name: 'Maionese', priceCents: 0),
              isIncludedByDefault: true,
            ),
          ],
        ),
        ProductOptionGroup(
          id: 'extras',
          name: 'Extras',
          options: <ProductOption>[
            ProductOption(sku: Sku(id: 'bacon', name: 'Bacon', priceCents: 300)),
            ProductOption(sku: Sku(id: 'extra_cheese', name: 'Queijo extra', priceCents: 250)),
          ],
        ),
      ],
    ),
    Product(
      id: 'hotdog',
      categoryId: 'snacks',
      name: 'Cachorro-quente',
      baseSku: Sku(
        id: 'hotdog',
        name: 'Cachorro-quente',
        priceCents: 1590,
        imageUrl: 'https://picsum.photos/seed/hotdog/600/450',
      ),
      optionGroups: <ProductOptionGroup>[
        ProductOptionGroup(
          id: 'ingredients',
          name: 'Ingredientes',
          options: <ProductOption>[
            ProductOption(
              sku: Sku(id: 'bun', name: 'Pão', priceCents: 0),
              isIncludedByDefault: true,
              isRemovable: false,
            ),
            ProductOption(
              sku: Sku(id: 'sausage', name: 'Salsicha', priceCents: 0),
              isIncludedByDefault: true,
              isRemovable: false,
            ),
            ProductOption(
              sku: Sku(id: 'potato', name: 'Batata palha', priceCents: 0),
              isIncludedByDefault: true,
            ),
            ProductOption(
              sku: Sku(id: 'ketchup', name: 'Ketchup', priceCents: 0),
              isIncludedByDefault: true,
            ),
            ProductOption(
              sku: Sku(id: 'mustard', name: 'Mostarda', priceCents: 0),
              isIncludedByDefault: true,
            ),
            ProductOption(
              sku: Sku(id: 'mayo', name: 'Maionese', priceCents: 0),
              isIncludedByDefault: true,
            ),
          ],
        ),
        ProductOptionGroup(
          id: 'extras',
          name: 'Extras',
          options: <ProductOption>[
            ProductOption(sku: Sku(id: 'cheddar', name: 'Cheddar', priceCents: 250)),
          ],
        ),
      ],
    ),
    Product(
      id: 'fries',
      categoryId: 'snacks',
      name: 'Batata frita',
      baseSku: Sku(
        id: 'fries',
        name: 'Batata frita',
        priceCents: 1290,
        imageUrl: 'https://picsum.photos/seed/fries/600/450',
      ),
    ),
    Product(
      id: 'combo1',
      categoryId: 'snacks',
      name: 'Combo Lanche',
      baseSku: Sku(
        id: 'combo1',
        name: 'Combo Lanche',
        priceCents: 2790,
        imageUrl: 'https://picsum.photos/seed/combo/600/450',
      ),
    ),
    Product(
      id: 'dish1',
      categoryId: 'meals',
      name: 'Prato do dia',
      baseSku: Sku(
        id: 'dish1',
        name: 'Prato do dia',
        priceCents: 2890,
        imageUrl: 'https://picsum.photos/seed/dish1/600/450',
      ),
    ),
    Product(
      id: 'dish2',
      categoryId: 'meals',
      name: 'Executivo',
      baseSku: Sku(
        id: 'dish2',
        name: 'Executivo',
        priceCents: 3190,
        imageUrl: 'https://picsum.photos/seed/dish2/600/450',
      ),
    ),
    Product(
      id: 'salad',
      categoryId: 'meals',
      name: 'Salada',
      baseSku: Sku(
        id: 'salad',
        name: 'Salada',
        priceCents: 2490,
        imageUrl: 'https://picsum.photos/seed/salad/600/450',
      ),
    ),
    Product(
      id: 'pasta',
      categoryId: 'meals',
      name: 'Massa',
      baseSku: Sku(
        id: 'pasta',
        name: 'Massa',
        priceCents: 3390,
        imageUrl: 'https://picsum.photos/seed/pasta/600/450',
      ),
    ),
    Product(
      id: 'icecream',
      categoryId: 'desserts',
      name: 'Sorvete',
      baseSku: Sku(
        id: 'icecream',
        name: 'Sorvete',
        priceCents: 1090,
        imageUrl: 'https://picsum.photos/seed/icecream/600/450',
      ),
    ),
    Product(
      id: 'pie',
      categoryId: 'desserts',
      name: 'Torta',
      baseSku: Sku(
        id: 'pie',
        name: 'Torta',
        priceCents: 1290,
        imageUrl: 'https://picsum.photos/seed/pie/600/450',
      ),
    ),
    Product(
      id: 'brownie',
      categoryId: 'desserts',
      name: 'Brownie',
      baseSku: Sku(
        id: 'brownie',
        name: 'Brownie',
        priceCents: 1390,
        imageUrl: 'https://picsum.photos/seed/brownie/600/450',
      ),
    ),
    Product(
      id: 'pudding',
      categoryId: 'desserts',
      name: 'Pudim',
      baseSku: Sku(
        id: 'pudding',
        name: 'Pudim',
        priceCents: 1190,
        imageUrl: 'https://picsum.photos/seed/pudding/600/450',
      ),
    ),
  ];

  @override
  Future<List<KioskCategory>> getCategories() async {
    return _categories;
  }

  @override
  Future<List<Product>> getProductsForCategory(String categoryId) async {
    return _products.where((p) => p.categoryId == categoryId).toList(growable: false);
  }
}
