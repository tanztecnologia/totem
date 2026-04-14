abstract class CatalogAdminRepository {
  Future<CatalogAdminSkuResult> createSku({
    required String categoryCode,
    required String name,
    required int priceCents,
    int? averagePrepSeconds,
    String? imageUrl,
    bool isActive = true,
  });

  Future<CatalogAdminSkuResult?> getSkuByCode({
    required String code,
  });

  Future<CatalogAdminSkuSearchPage> searchSkus({
    String? query,
    int limit = 50,
    String? cursorCode,
    String? cursorId,
    bool includeInactive = true,
  });

  Future<CatalogAdminSkuResult> updateSku({
    required String id,
    required String categoryCode,
    required String name,
    required int priceCents,
    int? averagePrepSeconds,
    String? imageUrl,
    required bool isActive,
  });

  Future<CatalogAdminCategoryResult> createCategory({
    required String name,
    String? slug,
    bool isActive = true,
  });

  Future<List<CatalogAdminCategoryResult>> listCategories({
    bool includeInactive = true,
  });
}

class CatalogAdminSkuResult {
  const CatalogAdminSkuResult({
    required this.id,
    required this.categoryCode,
    required this.code,
    required this.name,
    required this.priceCents,
    required this.averagePrepSeconds,
    required this.imageUrl,
    required this.isActive,
  });

  final String id;
  final String categoryCode;
  final String code;
  final String name;
  final int priceCents;
  final int? averagePrepSeconds;
  final String? imageUrl;
  final bool isActive;
}

class CatalogAdminSkuSearchPage {
  const CatalogAdminSkuSearchPage({
    required this.items,
    required this.nextCursorCode,
    required this.nextCursorId,
  });

  final List<CatalogAdminSkuResult> items;
  final String? nextCursorCode;
  final String? nextCursorId;

  bool get hasMore => nextCursorCode != null && nextCursorId != null && nextCursorId!.trim().isNotEmpty;
}

class CatalogAdminCategoryResult {
  const CatalogAdminCategoryResult({
    required this.id,
    required this.code,
    required this.slug,
    required this.name,
    required this.isActive,
  });

  final String id;
  final String code;
  final String slug;
  final String name;
  final bool isActive;
}
