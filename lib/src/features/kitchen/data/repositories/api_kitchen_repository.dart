import 'package:totem/src/http/totem_http.dart';

import '../../domain/entities/kitchen_order.dart';
import '../../domain/repositories/kitchen_repository.dart';

class ApiKitchenRepository implements KitchenRepository {
  final Uri baseUrl;
  final String tenantName;
  final String email;
  final String password;

  late final TotemHttpClient _http;
  String? _token;
  bool _reAuthAttempted = false;

  ApiKitchenRepository({
    required this.baseUrl,
    required this.tenantName,
    required this.email,
    required this.password,
    String? initialToken,
  }) {
    _token = initialToken?.trim().isEmpty ?? true ? null : initialToken!.trim();
    _http = TotemHttpClient(
      baseUrl: baseUrl,
      tokenProvider: () => _token,
    );
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
        throw Exception('Credenciais inválidas para o modo cozinha');
      }
      throw Exception('Falha ao autenticar na cozinha ($e)');
    }
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

  @override
  Future<List<KitchenOrder>> listOrders({List<KitchenOrderStatus>? statuses}) async {
    try {
      final queryParameters = statuses == null || statuses.isEmpty
          ? null
          : <String, dynamic>{
              'status': statuses.map((s) => s.name).toList(),
            };

      final data = await _authedGet<List<dynamic>>(
        '/api/kitchen/orders',
        queryParameters: queryParameters,
      );

      return data
          .whereType<Map>()
          .map((e) => KitchenOrder.fromJson(e.cast<String, dynamic>()))
          .toList();
    } on TotemHttpException catch (e) {
      if (e.statusCode == 401) {
        _token = null;
        throw Exception('Não autenticado na API. Verifique baseUrl/tenant/email/senha.');
      }
      if (e.statusCode == 403) {
        throw Exception('Acesso negado: usuário sem permissão de cozinha');
      }
      throw Exception('Falha ao carregar pedidos da cozinha ($e)');
    }
  }

  @override
  Future<KitchenOrder?> getOrder(String orderId) async {
    try {
      final data = await _authedGet<Map<String, dynamic>>('/api/kitchen/orders/$orderId');
      return KitchenOrder.fromJson(data);
    } on TotemHttpException catch (e) {
      if (e.statusCode == 404) return null;
      if (e.statusCode == 401) {
        _token = null;
        throw Exception('Não autenticado na API. Verifique baseUrl/tenant/email/senha.');
      }
      if (e.statusCode == 403) {
        throw Exception('Acesso negado: usuário sem permissão de cozinha');
      }
      throw Exception('Falha ao buscar pedido $orderId ($e)');
    }
  }

  @override
  Future<void> updateOrderStatus(String orderId, KitchenOrderStatus newStatus) async {
    try {
      await _ensureToken();
      await _http.postJson<Object?>(
        '/api/kitchen/orders/$orderId/status',
        body: <String, Object?>{
          'kitchenStatus': _toApiKitchenStatus(newStatus),
        },
      );
    } on TotemHttpException catch (e) {
      if (e.statusCode == 401) {
        _token = null;
        throw Exception('Não autenticado na API. Verifique baseUrl/tenant/email/senha.');
      }
      if (e.statusCode == 403) {
        throw Exception('Acesso negado: usuário sem permissão de cozinha');
      }
      throw Exception('Falha ao atualizar status do pedido ($e)');
    }
  }

  String _toApiKitchenStatus(KitchenOrderStatus status) {
    switch (status) {
      case KitchenOrderStatus.pendingPayment:
        return 'PendingPayment';
      case KitchenOrderStatus.queued:
        return 'Queued';
      case KitchenOrderStatus.inPreparation:
        return 'InPreparation';
      case KitchenOrderStatus.ready:
        return 'Ready';
      case KitchenOrderStatus.completed:
        return 'Completed';
      case KitchenOrderStatus.cancelled:
        return 'Cancelled';
    }
  }
}
