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

  @override
  Future<CatalogAdminSkuResult> createSku({
    required String categoryCode,
    required String name,
    required int priceCents,
    int? averagePrepSeconds,
    String? imageUrl,
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
      isActive: (resp['isActive'] as bool?) ?? isActive,
    );
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
