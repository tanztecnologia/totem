import 'package:totem/src/http/totem_http.dart';

import '../../domain/repositories/catalog_admin_repository.dart';

class TotemApiCatalogAdminRepository implements CatalogAdminRepository {
  TotemApiCatalogAdminRepository({
    required Uri baseUrl,
    required String token,
    TotemHttpClient? httpClient,
  })  : _token = token,
        _http = httpClient ??
            TotemHttpClient(
              baseUrl: baseUrl,
              tokenProvider: () => token,
            );

  final TotemHttpClient _http;
  final String _token;

  int? _parseStockBaseUnit(Object? raw) {
    if (raw == null) return null;
    if (raw is num) return raw.toInt();
    final s = raw.toString().trim();
    if (s.isEmpty) return null;
    final lower = s.toLowerCase();
    if (lower == 'unit' || lower == 'unidade') return 0;
    if (lower == 'gram' || lower == 'grama') return 1;
    if (lower == 'milliliter' || lower == 'mililitro') return 2;
    if (int.tryParse(s, radix: 10) case final int n) return n;
    return null;
  }

  @override
  Future<CatalogAdminSkuResult> createSku({
    required String categoryCode,
    required String name,
    required int priceCents,
    int? averagePrepSeconds,
    String? imageUrl,
    int? stockBaseUnit,
    num? stockOnHandBaseQty,
    bool isActive = true,
  }) async {
    if (_token.trim().isEmpty) throw Exception('Sessão inválida (token ausente).');

    final resp = await _http.postJson<Map<String, dynamic>>(
      '/api/skus',
      body: <String, Object?>{
        'categoryCode': categoryCode.trim(),
        'name': name.trim(),
        'priceCents': priceCents,
        'averagePrepSeconds': averagePrepSeconds,
        'imageUrl': imageUrl?.trim().isEmpty ?? true ? null : imageUrl!.trim(),
        if (stockBaseUnit != null) 'stockBaseUnit': stockBaseUnit,
        if (stockOnHandBaseQty != null) 'stockOnHandBaseQty': stockOnHandBaseQty,
        'isActive': isActive,
      },
    );

    return CatalogAdminSkuResult(
      id: resp['id']?.toString() ?? '',
      categoryCode: resp['categoryCode']?.toString() ?? categoryCode.trim(),
      code: resp['code']?.toString() ?? '',
      name: resp['name']?.toString() ?? name.trim(),
      priceCents: (resp['priceCents'] as num?)?.toInt() ?? priceCents,
      averagePrepSeconds: (resp['averagePrepSeconds'] as num?)?.toInt(),
      imageUrl: resp['imageUrl']?.toString(),
      stockBaseUnit: _parseStockBaseUnit(resp['stockBaseUnit']),
      stockOnHandBaseQty: resp['stockOnHandBaseQty'] as num?,
      nfeCProd: resp['nfeCProd']?.toString(),
      nfeCEan: resp['nfeCEan']?.toString(),
      nfeCfop: resp['nfeCfop']?.toString(),
      nfeUCom: resp['nfeUCom']?.toString(),
      nfeQCom: resp['nfeQCom'] as num?,
      nfeVUnCom: resp['nfeVUnCom'] as num?,
      nfeVProd: resp['nfeVProd'] as num?,
      nfeCEanTrib: resp['nfeCEanTrib']?.toString(),
      nfeUTrib: resp['nfeUTrib']?.toString(),
      nfeQTrib: resp['nfeQTrib'] as num?,
      nfeVUnTrib: resp['nfeVUnTrib'] as num?,
      nfeIcmsOrig: resp['nfeIcmsOrig']?.toString(),
      nfeIcmsCst: resp['nfeIcmsCst']?.toString(),
      nfeIcmsModBc: resp['nfeIcmsModBc']?.toString(),
      nfeIcmsVBc: resp['nfeIcmsVBc'] as num?,
      nfeIcmsPIcms: resp['nfeIcmsPIcms'] as num?,
      nfeIcmsVIcms: resp['nfeIcmsVIcms'] as num?,
      nfePisCst: resp['nfePisCst']?.toString(),
      nfePisVBc: resp['nfePisVBc'] as num?,
      nfePisPPis: resp['nfePisPPis'] as num?,
      nfePisVPis: resp['nfePisVPis'] as num?,
      nfeCofinsCst: resp['nfeCofinsCst']?.toString(),
      nfeCofinsVBc: resp['nfeCofinsVBc'] as num?,
      nfeCofinsPCofins: resp['nfeCofinsPCofins'] as num?,
      nfeCofinsVCofins: resp['nfeCofinsVCofins'] as num?,
      isActive: (resp['isActive'] as bool?) ?? isActive,
    );
  }

  @override
  Future<CatalogAdminSkuResult?> getSkuByCode({
    required String code,
  }) async {
    if (_token.trim().isEmpty) throw Exception('Sessão inválida (token ausente).');
    final trimmed = code.trim();
    if (trimmed.isEmpty) throw Exception('Informe o código do SKU.');

    try {
      final resp = await _http.getJson<Map<String, dynamic>>(
        '/api/skus/by-code',
        queryParameters: <String, dynamic>{
          'code': trimmed,
        },
      );

      return CatalogAdminSkuResult(
        id: resp['id']?.toString() ?? '',
        categoryCode: resp['categoryCode']?.toString() ?? '',
        code: resp['code']?.toString() ?? trimmed,
        name: resp['name']?.toString() ?? '',
        priceCents: (resp['priceCents'] as num?)?.toInt() ?? 0,
        averagePrepSeconds: (resp['averagePrepSeconds'] as num?)?.toInt(),
        imageUrl: resp['imageUrl']?.toString(),
        stockBaseUnit: _parseStockBaseUnit(resp['stockBaseUnit']),
        stockOnHandBaseQty: resp['stockOnHandBaseQty'] as num?,
        nfeCProd: resp['nfeCProd']?.toString(),
        nfeCEan: resp['nfeCEan']?.toString(),
        nfeCfop: resp['nfeCfop']?.toString(),
        nfeUCom: resp['nfeUCom']?.toString(),
        nfeQCom: resp['nfeQCom'] as num?,
        nfeVUnCom: resp['nfeVUnCom'] as num?,
        nfeVProd: resp['nfeVProd'] as num?,
        nfeCEanTrib: resp['nfeCEanTrib']?.toString(),
        nfeUTrib: resp['nfeUTrib']?.toString(),
        nfeQTrib: resp['nfeQTrib'] as num?,
        nfeVUnTrib: resp['nfeVUnTrib'] as num?,
        nfeIcmsOrig: resp['nfeIcmsOrig']?.toString(),
        nfeIcmsCst: resp['nfeIcmsCst']?.toString(),
        nfeIcmsModBc: resp['nfeIcmsModBc']?.toString(),
        nfeIcmsVBc: resp['nfeIcmsVBc'] as num?,
        nfeIcmsPIcms: resp['nfeIcmsPIcms'] as num?,
        nfeIcmsVIcms: resp['nfeIcmsVIcms'] as num?,
        nfePisCst: resp['nfePisCst']?.toString(),
        nfePisVBc: resp['nfePisVBc'] as num?,
        nfePisPPis: resp['nfePisPPis'] as num?,
        nfePisVPis: resp['nfePisVPis'] as num?,
        nfeCofinsCst: resp['nfeCofinsCst']?.toString(),
        nfeCofinsVBc: resp['nfeCofinsVBc'] as num?,
        nfeCofinsPCofins: resp['nfeCofinsPCofins'] as num?,
        nfeCofinsVCofins: resp['nfeCofinsVCofins'] as num?,
        isActive: (resp['isActive'] as bool?) ?? true,
      );
    } on TotemHttpException catch (e) {
      if (e.statusCode == 404) return null;
      rethrow;
    }
  }

  @override
  Future<CatalogAdminSkuSearchPage> searchSkus({
    String? query,
    int limit = 50,
    String? cursorCode,
    String? cursorId,
    bool includeInactive = true,
  }) async {
    if (_token.trim().isEmpty) throw Exception('Sessão inválida (token ausente).');

    final resp = await _http.getJson<Map<String, dynamic>>(
      '/api/skus/search',
      queryParameters: <String, dynamic>{
        if (query != null && query.trim().isNotEmpty) 'query': query.trim(),
        'limit': limit,
        if (cursorCode != null && cursorCode.trim().isNotEmpty) 'cursorCode': cursorCode.trim(),
        if (cursorId != null && cursorId.trim().isNotEmpty) 'cursorId': cursorId.trim(),
        'includeInactive': includeInactive,
      },
    );

    final itemsRaw = (resp['items'] as List?) ?? const [];
    final items = itemsRaw
        .map((e) => (e as Map).cast<String, Object?>())
        .map(
          (m) => CatalogAdminSkuResult(
            id: m['id']?.toString() ?? '',
            categoryCode: m['categoryCode']?.toString() ?? '',
            code: m['code']?.toString() ?? '',
            name: m['name']?.toString() ?? '',
            priceCents: (m['priceCents'] as num?)?.toInt() ?? 0,
            averagePrepSeconds: (m['averagePrepSeconds'] as num?)?.toInt(),
            imageUrl: m['imageUrl']?.toString(),
            stockBaseUnit: _parseStockBaseUnit(m['stockBaseUnit']),
            stockOnHandBaseQty: m['stockOnHandBaseQty'] as num?,
            nfeCProd: m['nfeCProd']?.toString(),
            nfeCEan: m['nfeCEan']?.toString(),
            nfeCfop: m['nfeCfop']?.toString(),
            nfeUCom: m['nfeUCom']?.toString(),
            nfeQCom: m['nfeQCom'] as num?,
            nfeVUnCom: m['nfeVUnCom'] as num?,
            nfeVProd: m['nfeVProd'] as num?,
            nfeCEanTrib: m['nfeCEanTrib']?.toString(),
            nfeUTrib: m['nfeUTrib']?.toString(),
            nfeQTrib: m['nfeQTrib'] as num?,
            nfeVUnTrib: m['nfeVUnTrib'] as num?,
            nfeIcmsOrig: m['nfeIcmsOrig']?.toString(),
            nfeIcmsCst: m['nfeIcmsCst']?.toString(),
            nfeIcmsModBc: m['nfeIcmsModBc']?.toString(),
            nfeIcmsVBc: m['nfeIcmsVBc'] as num?,
            nfeIcmsPIcms: m['nfeIcmsPIcms'] as num?,
            nfeIcmsVIcms: m['nfeIcmsVIcms'] as num?,
            nfePisCst: m['nfePisCst']?.toString(),
            nfePisVBc: m['nfePisVBc'] as num?,
            nfePisPPis: m['nfePisPPis'] as num?,
            nfePisVPis: m['nfePisVPis'] as num?,
            nfeCofinsCst: m['nfeCofinsCst']?.toString(),
            nfeCofinsVBc: m['nfeCofinsVBc'] as num?,
            nfeCofinsPCofins: m['nfeCofinsPCofins'] as num?,
            nfeCofinsVCofins: m['nfeCofinsVCofins'] as num?,
            isActive: (m['isActive'] as bool?) ?? true,
          ),
        )
        .where((s) => s.id.trim().isNotEmpty)
        .toList(growable: false);

    return CatalogAdminSkuSearchPage(
      items: items,
      nextCursorCode: resp['nextCursorCode']?.toString(),
      nextCursorId: resp['nextCursorId']?.toString(),
    );
  }

  @override
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
  }) async {
    if (_token.trim().isEmpty) throw Exception('Sessão inválida (token ausente).');
    final trimmedId = id.trim();
    if (trimmedId.isEmpty) throw Exception('SKU inválido.');

    final resp = await _http.putJson<Map<String, dynamic>>(
      '/api/skus/$trimmedId',
      body: <String, Object?>{
        'categoryCode': categoryCode.trim(),
        'name': name.trim(),
        'priceCents': priceCents,
        'averagePrepSeconds': averagePrepSeconds,
        'imageUrl': imageUrl?.trim().isEmpty ?? true ? null : imageUrl!.trim(),
        if (stockBaseUnit != null) 'stockBaseUnit': stockBaseUnit,
        if (stockOnHandBaseQty != null) 'stockOnHandBaseQty': stockOnHandBaseQty,
        'isActive': isActive,
      },
    );

    return CatalogAdminSkuResult(
      id: resp['id']?.toString() ?? trimmedId,
      categoryCode: resp['categoryCode']?.toString() ?? categoryCode.trim(),
      code: resp['code']?.toString() ?? '',
      name: resp['name']?.toString() ?? name.trim(),
      priceCents: (resp['priceCents'] as num?)?.toInt() ?? priceCents,
      averagePrepSeconds: (resp['averagePrepSeconds'] as num?)?.toInt(),
      imageUrl: resp['imageUrl']?.toString(),
      stockBaseUnit: _parseStockBaseUnit(resp['stockBaseUnit']),
      stockOnHandBaseQty: resp['stockOnHandBaseQty'] as num?,
      nfeCProd: resp['nfeCProd']?.toString(),
      nfeCEan: resp['nfeCEan']?.toString(),
      nfeCfop: resp['nfeCfop']?.toString(),
      nfeUCom: resp['nfeUCom']?.toString(),
      nfeQCom: resp['nfeQCom'] as num?,
      nfeVUnCom: resp['nfeVUnCom'] as num?,
      nfeVProd: resp['nfeVProd'] as num?,
      nfeCEanTrib: resp['nfeCEanTrib']?.toString(),
      nfeUTrib: resp['nfeUTrib']?.toString(),
      nfeQTrib: resp['nfeQTrib'] as num?,
      nfeVUnTrib: resp['nfeVUnTrib'] as num?,
      nfeIcmsOrig: resp['nfeIcmsOrig']?.toString(),
      nfeIcmsCst: resp['nfeIcmsCst']?.toString(),
      nfeIcmsModBc: resp['nfeIcmsModBc']?.toString(),
      nfeIcmsVBc: resp['nfeIcmsVBc'] as num?,
      nfeIcmsPIcms: resp['nfeIcmsPIcms'] as num?,
      nfeIcmsVIcms: resp['nfeIcmsVIcms'] as num?,
      nfePisCst: resp['nfePisCst']?.toString(),
      nfePisVBc: resp['nfePisVBc'] as num?,
      nfePisPPis: resp['nfePisPPis'] as num?,
      nfePisVPis: resp['nfePisVPis'] as num?,
      nfeCofinsCst: resp['nfeCofinsCst']?.toString(),
      nfeCofinsVBc: resp['nfeCofinsVBc'] as num?,
      nfeCofinsPCofins: resp['nfeCofinsPCofins'] as num?,
      nfeCofinsVCofins: resp['nfeCofinsVCofins'] as num?,
      isActive: (resp['isActive'] as bool?) ?? isActive,
    );
  }

  @override
  Future<CatalogAdminSkuResult> addSkuStockEntry({
    required String id,
    required num quantity,
    required String unit,
  }) async {
    if (_token.trim().isEmpty) throw Exception('Sessão inválida (token ausente).');
    final trimmedId = id.trim();
    if (trimmedId.isEmpty) throw Exception('SKU inválido.');

    final resp = await _http.postJson<Map<String, dynamic>>(
      '/api/skus/$trimmedId/stock/entry',
      body: <String, Object?>{
        'quantity': quantity,
        'unit': unit.trim(),
      },
    );

    return CatalogAdminSkuResult(
      id: resp['id']?.toString() ?? trimmedId,
      categoryCode: resp['categoryCode']?.toString() ?? '',
      code: resp['code']?.toString() ?? '',
      name: resp['name']?.toString() ?? '',
      priceCents: (resp['priceCents'] as num?)?.toInt() ?? 0,
      averagePrepSeconds: (resp['averagePrepSeconds'] as num?)?.toInt(),
      imageUrl: resp['imageUrl']?.toString(),
      stockBaseUnit: _parseStockBaseUnit(resp['stockBaseUnit']),
      stockOnHandBaseQty: resp['stockOnHandBaseQty'] as num?,
      nfeCProd: resp['nfeCProd']?.toString(),
      nfeCEan: resp['nfeCEan']?.toString(),
      nfeCfop: resp['nfeCfop']?.toString(),
      nfeUCom: resp['nfeUCom']?.toString(),
      nfeQCom: resp['nfeQCom'] as num?,
      nfeVUnCom: resp['nfeVUnCom'] as num?,
      nfeVProd: resp['nfeVProd'] as num?,
      nfeCEanTrib: resp['nfeCEanTrib']?.toString(),
      nfeUTrib: resp['nfeUTrib']?.toString(),
      nfeQTrib: resp['nfeQTrib'] as num?,
      nfeVUnTrib: resp['nfeVUnTrib'] as num?,
      nfeIcmsOrig: resp['nfeIcmsOrig']?.toString(),
      nfeIcmsCst: resp['nfeIcmsCst']?.toString(),
      nfeIcmsModBc: resp['nfeIcmsModBc']?.toString(),
      nfeIcmsVBc: resp['nfeIcmsVBc'] as num?,
      nfeIcmsPIcms: resp['nfeIcmsPIcms'] as num?,
      nfeIcmsVIcms: resp['nfeIcmsVIcms'] as num?,
      nfePisCst: resp['nfePisCst']?.toString(),
      nfePisVBc: resp['nfePisVBc'] as num?,
      nfePisPPis: resp['nfePisPPis'] as num?,
      nfePisVPis: resp['nfePisVPis'] as num?,
      nfeCofinsCst: resp['nfeCofinsCst']?.toString(),
      nfeCofinsVBc: resp['nfeCofinsVBc'] as num?,
      nfeCofinsPCofins: resp['nfeCofinsPCofins'] as num?,
      nfeCofinsVCofins: resp['nfeCofinsVCofins'] as num?,
      isActive: (resp['isActive'] as bool?) ?? true,
    );
  }

  @override
  Future<List<CatalogAdminSkuStockConsumption>> listSkuStockConsumptions({required String id}) async {
    if (_token.trim().isEmpty) throw Exception('Sessão inválida (token ausente).');
    final trimmedId = id.trim();
    if (trimmedId.isEmpty) throw Exception('SKU inválido.');

    final resp = await _http.getJson<List<dynamic>>('/api/skus/$trimmedId/stock/consumptions');
    return resp
        .whereType<Map>()
        .map((e) => e.cast<String, Object?>())
        .map(
          (m) => CatalogAdminSkuStockConsumption(
            id: m['id']?.toString() ?? '',
            skuId: m['skuId']?.toString() ?? '',
            sourceSkuId: m['sourceSkuId']?.toString() ?? '',
            sourceSkuCode: m['sourceSkuCode']?.toString() ?? '',
            quantityBase: (m['quantityBase'] as num?) ?? 0,
          ),
        )
        .where((x) => x.id.trim().isNotEmpty)
        .toList(growable: false);
  }

  @override
  Future<List<CatalogAdminSkuStockConsumption>> replaceSkuStockConsumptions({
    required String id,
    required List<({String sourceSkuCode, num quantity, String unit})> items,
  }) async {
    if (_token.trim().isEmpty) throw Exception('Sessão inválida (token ausente).');
    final trimmedId = id.trim();
    if (trimmedId.isEmpty) throw Exception('SKU inválido.');

    final resp = await _http.putJson<List<dynamic>>(
      '/api/skus/$trimmedId/stock/consumptions',
      body: <String, Object?>{
        'items': items
            .map(
              (x) => <String, Object?>{
                'sourceSkuCode': x.sourceSkuCode.trim(),
                'quantity': x.quantity,
                'unit': x.unit.trim(),
              },
            )
            .toList(growable: false),
      },
    );

    return resp
        .whereType<Map>()
        .map((e) => e.cast<String, Object?>())
        .map(
          (m) => CatalogAdminSkuStockConsumption(
            id: m['id']?.toString() ?? '',
            skuId: m['skuId']?.toString() ?? '',
            sourceSkuId: m['sourceSkuId']?.toString() ?? '',
            sourceSkuCode: m['sourceSkuCode']?.toString() ?? '',
            quantityBase: (m['quantityBase'] as num?) ?? 0,
          ),
        )
        .where((x) => x.id.trim().isNotEmpty)
        .toList(growable: false);
  }

  @override
  Future<CatalogAdminCategoryResult> createCategory({
    required String name,
    String? slug,
    bool isActive = true,
  }) async {
    if (_token.trim().isEmpty) throw Exception('Sessão inválida (token ausente).');

    final resp = await _http.postJson<Map<String, dynamic>>(
      '/api/categories',
      body: <String, Object?>{
        'name': name.trim(),
        'slug': slug?.trim().isEmpty ?? true ? null : slug!.trim(),
        'isActive': isActive,
      },
    );

    return CatalogAdminCategoryResult(
      id: resp['id']?.toString() ?? '',
      code: resp['code']?.toString() ?? '',
      slug: resp['slug']?.toString() ?? '',
      name: resp['name']?.toString() ?? name.trim(),
      isActive: (resp['isActive'] as bool?) ?? isActive,
    );
  }

  @override
  Future<List<CatalogAdminCategoryResult>> listCategories({bool includeInactive = true}) async {
    if (_token.trim().isEmpty) throw Exception('Sessão inválida (token ausente).');

    final resp = await _http.getJson<List<dynamic>>(
      '/api/categories',
      queryParameters: <String, dynamic>{
        'includeInactive': includeInactive,
      },
    );

    return resp
        .whereType<Map>()
        .map((e) => e.cast<String, Object?>())
        .map(
          (m) => CatalogAdminCategoryResult(
            id: m['id']?.toString() ?? '',
            code: m['code']?.toString() ?? '',
            slug: m['slug']?.toString() ?? '',
            name: m['name']?.toString() ?? '',
            isActive: (m['isActive'] as bool?) ?? true,
          ),
        )
        .where((c) => c.code.trim().isNotEmpty)
        .toList(growable: false);
  }
}
