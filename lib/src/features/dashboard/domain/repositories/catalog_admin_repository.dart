abstract class CatalogAdminRepository {
  Future<CatalogAdminSkuResult> createSku({
    required String categoryCode,
    required String name,
    required int priceCents,
    int? averagePrepSeconds,
    String? imageUrl,
    int? stockBaseUnit,
    num? stockOnHandBaseQty,
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
    int? stockBaseUnit,
    num? stockOnHandBaseQty,
    required bool isActive,
  });

  Future<CatalogAdminSkuResult> addSkuStockEntry({
    required String id,
    required num quantity,
    required String unit,
  });

  Future<List<CatalogAdminSkuStockConsumption>> listSkuStockConsumptions({
    required String id,
  });

  Future<List<CatalogAdminSkuStockConsumption>> replaceSkuStockConsumptions({
    required String id,
    required List<({String sourceSkuCode, num quantity, String unit})> items,
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
    required this.stockBaseUnit,
    required this.stockOnHandBaseQty,
    required this.nfeCProd,
    required this.nfeCEan,
    required this.nfeCfop,
    required this.nfeUCom,
    required this.nfeQCom,
    required this.nfeVUnCom,
    required this.nfeVProd,
    required this.nfeCEanTrib,
    required this.nfeUTrib,
    required this.nfeQTrib,
    required this.nfeVUnTrib,
    required this.nfeIcmsOrig,
    required this.nfeIcmsCst,
    required this.nfeIcmsModBc,
    required this.nfeIcmsVBc,
    required this.nfeIcmsPIcms,
    required this.nfeIcmsVIcms,
    required this.nfePisCst,
    required this.nfePisVBc,
    required this.nfePisPPis,
    required this.nfePisVPis,
    required this.nfeCofinsCst,
    required this.nfeCofinsVBc,
    required this.nfeCofinsPCofins,
    required this.nfeCofinsVCofins,
    required this.isActive,
  });

  final String id;
  final String categoryCode;
  final String code;
  final String name;
  final int priceCents;
  final int? averagePrepSeconds;
  final String? imageUrl;
  final int? stockBaseUnit;
  final num? stockOnHandBaseQty;
  final String? nfeCProd;
  final String? nfeCEan;
  final String? nfeCfop;
  final String? nfeUCom;
  final num? nfeQCom;
  final num? nfeVUnCom;
  final num? nfeVProd;
  final String? nfeCEanTrib;
  final String? nfeUTrib;
  final num? nfeQTrib;
  final num? nfeVUnTrib;
  final String? nfeIcmsOrig;
  final String? nfeIcmsCst;
  final String? nfeIcmsModBc;
  final num? nfeIcmsVBc;
  final num? nfeIcmsPIcms;
  final num? nfeIcmsVIcms;
  final String? nfePisCst;
  final num? nfePisVBc;
  final num? nfePisPPis;
  final num? nfePisVPis;
  final String? nfeCofinsCst;
  final num? nfeCofinsVBc;
  final num? nfeCofinsPCofins;
  final num? nfeCofinsVCofins;
  final bool isActive;
}

class CatalogAdminSkuStockConsumption {
  const CatalogAdminSkuStockConsumption({
    required this.id,
    required this.skuId,
    required this.sourceSkuId,
    required this.sourceSkuCode,
    required this.quantityBase,
  });

  final String id;
  final String skuId;
  final String sourceSkuId;
  final String sourceSkuCode;
  final num quantityBase;
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
