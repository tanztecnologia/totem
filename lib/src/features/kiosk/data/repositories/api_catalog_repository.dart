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
    TotemHttpClient? httpClient,
  }) {
    _http = httpClient ??
        TotemHttpClient(
          baseUrl: baseUrl,
          tokenProvider: () => _token,
        );
  }

  @override
  Future<List<KioskCategory>> getCategories() async {
    final skus = await _listActiveSkus();
    return mapSkusToCategories(skus);
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
List<KioskCategory> mapSkusToCategories(List<ApiSkuDto> skus) {
  final ids = skus.map((s) => categoryIdFromSkuCode(s.code)).toSet();
  final ordered = ids.toList()..sort();

  return ordered
      .map(
        (id) => KioskCategory(
          id: id,
          name: displayNameFromCategoryId(id),
        ),
      )
      .toList(growable: false);
}

@visibleForTesting
List<kiosk.Product> mapSkusToProductsForCategory(
  List<ApiSkuDto> skus,
  String categoryId,
) {
  final products = skus
      .where((s) => categoryIdFromSkuCode(s.code) == categoryId)
      .map(
        (s) => kiosk.Product(
          id: s.code,
          categoryId: categoryIdFromSkuCode(s.code),
          name: s.name,
          baseSku: kiosk.Sku(
            id: s.code,
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

@visibleForTesting
String categoryIdFromSkuCode(String code) {
  final trimmed = code.trim();
  if (trimmed.isEmpty) return 'outros';
  final parts = trimmed.split(RegExp(r'[-_ ]+')).where((p) => p.trim().isNotEmpty).toList();
  if (parts.isEmpty) return 'outros';
  return parts.first.toLowerCase();
}

@visibleForTesting
String displayNameFromCategoryId(String id) {
  final trimmed = id.trim();
  if (trimmed.isEmpty) return 'Outros';
  final lower = trimmed.toLowerCase();
  return lower.substring(0, 1).toUpperCase() + lower.substring(1);
}

class ApiSkuDto {
  ApiSkuDto({
    required this.id,
    required this.code,
    required this.name,
    required this.priceCents,
    required this.imageUrl,
    required this.isActive,
  });

  final String id;
  final String code;
  final String name;
  final int priceCents;
  final String? imageUrl;
  final bool isActive;

  factory ApiSkuDto.fromMap(Map<String, dynamic> json) {
    return ApiSkuDto(
      id: (json['id'] as String?) ?? '',
      code: ((json['code'] as String?) ?? '').trim().toUpperCase(),
      name: ((json['name'] as String?) ?? '').trim(),
      priceCents: (json['priceCents'] as num?)?.toInt() ?? 0,
      imageUrl: (json['imageUrl'] as String?)?.trim(),
      isActive: (json['isActive'] as bool?) ?? true,
    );
  }
}
