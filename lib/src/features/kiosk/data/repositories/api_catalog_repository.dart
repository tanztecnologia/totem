import 'package:flutter/foundation.dart';
import 'package:totem/src/http/totem_http.dart';

import '../../domain/entities/category.dart';
import '../../domain/entities/product.dart' as kiosk;
import '../../domain/repositories/catalog_repository.dart';

class ApiCatalogRepository implements CatalogRepository {
  final Uri baseUrl;
  final String tenantName;
  final String email;
  final String password;

  late final TotemHttpClient _http;
  String? _token;
  bool _reAuthAttempted = false;

  List<ApiSkuDto>? _cachedSkus;
  DateTime? _cachedAt;

  ApiCatalogRepository({
    required this.baseUrl,
    required this.tenantName,
    required this.email,
    required this.password,
    String? initialToken,
    TotemHttpClient? httpClient,
  }) {
    _token = initialToken?.trim().isEmpty ?? true ? null : initialToken!.trim();
    _http = httpClient ??
        TotemHttpClient(
          baseUrl: baseUrl,
          tokenProvider: () => _token,
        );
  }

  @override
  Future<List<KioskCategory>> getCategories() async {
    final skus = await _listActiveSkus();
    final categoriesRaw = await _authedGet<List<dynamic>>(
      '/api/categories',
      queryParameters: <String, dynamic>{
        'includeInactive': false,
      },
    );
    final categories = categoriesRaw
        .whereType<Map>()
        .map((e) => ApiCategoryDto.fromMap(e.cast<String, dynamic>()))
        .toList(growable: false);
    return mapSkusToCategories(skus, categories);
  }

  @override
  Future<List<kiosk.Product>> getProductsForCategory(String categoryId) async {
    final skus = await _listActiveSkus();
    return mapSkusToProductsForCategory(skus, categoryId);
  }

  Future<List<ApiSkuDto>> _listActiveSkus() async {
    final all = await _listSkusCached();
    return all.where((s) => s.isActive).toList(growable: false);
  }

  Future<List<ApiSkuDto>> _listSkusCached() async {
    final cached = _cachedSkus;
    final cachedAt = _cachedAt;
    if (cached != null && cachedAt != null) {
      final age = DateTime.now().difference(cachedAt);
      if (age.inSeconds <= 30) return cached;
    }

    final fetched = await _authedGet<List<dynamic>>('/api/skus');
    final skus = fetched
        .whereType<Map>()
        .map((e) => ApiSkuDto.fromMap(e.cast<String, dynamic>()))
        .toList(growable: false);

    _cachedSkus = skus;
    _cachedAt = DateTime.now();
    return skus;
  }

  Future<T> _authedGet<T>(
    String path, {
    Map<String, dynamic>? queryParameters,
  }) async {
    try {
      await _ensureToken();
      return await _http.getJson<T>(path, queryParameters: queryParameters);
    } on TotemHttpException catch (e) {
      if (e.statusCode == 401 && !_reAuthAttempted) {
        _reAuthAttempted = true;
        _token = null;
        await _ensureToken();
        return await _http.getJson<T>(path, queryParameters: queryParameters);
      }
      rethrow;
    }
  }

  Future<void> _ensureToken() async {
    if (_token != null) return;

    try {
      final resp = await _http.postJson<Map<String, dynamic>>(
        '/api/auth/login',
        body: <String, Object?>{
          'tenantName': tenantName,
          'email': email,
          'password': password,
        },
      );

      final token = ((resp['token'] ?? resp['Token']) as String?)?.trim() ?? '';
      if (token.isEmpty) throw const TotemHttpException(message: 'Resposta inválida do login');

      _token = token;
      _reAuthAttempted = false;
    } on TotemHttpException catch (e) {
      if (e.statusCode == 401) {
        throw Exception('Credenciais inválidas para o modo totem');
      }
      throw Exception('Falha ao autenticar no totem ($e)');
    }
  }
}

@visibleForTesting
List<KioskCategory> mapSkusToCategories(List<ApiSkuDto> skus, List<ApiCategoryDto> categories) {
  final usedCategoryCodes = skus.map((s) => s.categoryCode).toSet();

  final availableCategories = categories.where((c) => usedCategoryCodes.contains(c.code)).toList();
  availableCategories.sort((a, b) => a.name.compareTo(b.name));

  return availableCategories
      .map((c) => KioskCategory(id: c.code, name: c.name))
      .toList(growable: false);
}

@visibleForTesting
List<kiosk.Product> mapSkusToProductsForCategory(
  List<ApiSkuDto> skus,
  String categoryId,
) {
  final products = skus
      .where((s) => s.categoryCode == categoryId)
      .map(
        (s) => kiosk.Product(
          id: s.id,
          categoryId: categoryId,
          name: s.name,
          baseSku: kiosk.Sku(
            id: s.id,
            name: s.name,
            priceCents: s.priceCents,
            imageUrl: s.imageUrl,
          ),
          optionGroups: const <kiosk.ProductOptionGroup>[],
        ),
      )
      .toList(growable: false);

  products.sort((a, b) => a.name.compareTo(b.name));
  return products;
}

class ApiSkuDto {
  ApiSkuDto({
    required this.id,
    required this.categoryCode,
    required this.code,
    required this.name,
    required this.priceCents,
    required this.imageUrl,
    required this.isActive,
  });

  final String id;
  final String categoryCode;
  final String code;
  final String name;
  final int priceCents;
  final String? imageUrl;
  final bool isActive;

  factory ApiSkuDto.fromMap(Map<String, dynamic> json) {
    return ApiSkuDto(
      id: (json['id'] as String?) ?? '',
      categoryCode: ((json['categoryCode'] as String?) ?? '').trim(),
      code: ((json['code'] as String?) ?? '').trim().toUpperCase(),
      name: ((json['name'] as String?) ?? '').trim(),
      priceCents: (json['priceCents'] as num?)?.toInt() ?? 0,
      imageUrl: (json['imageUrl'] as String?)?.trim(),
      isActive: (json['isActive'] as bool?) ?? true,
    );
  }
}

class ApiCategoryDto {
  ApiCategoryDto({
    required this.code,
    required this.slug,
    required this.name,
  });

  final String code;
  final String slug;
  final String name;

  factory ApiCategoryDto.fromMap(Map<String, dynamic> json) {
    return ApiCategoryDto(
      code: ((json['code'] as String?) ?? '').trim(),
      slug: ((json['slug'] as String?) ?? '').trim().toLowerCase(),
      name: ((json['name'] as String?) ?? '').trim(),
    );
  }
}
