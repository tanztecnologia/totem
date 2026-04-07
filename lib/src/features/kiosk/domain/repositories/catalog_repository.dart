import '../entities/category.dart';
import '../entities/product.dart';

abstract interface class CatalogRepository {
  Future<List<KioskCategory>> getCategories();
  Future<List<Product>> getProductsForCategory(String categoryId);
}
