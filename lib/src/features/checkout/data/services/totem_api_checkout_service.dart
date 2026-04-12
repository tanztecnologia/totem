import 'package:totem/src/http/totem_http.dart';

import '../../domain/entities/checkout_item.dart';
import '../../domain/entities/checkout_order.dart';
import '../../domain/entities/payment_result.dart';
import '../../domain/entities/pix_charge.dart';
import '../../domain/services/checkout_service.dart';

class TotemApiCheckoutService implements CheckoutService {
  TotemApiCheckoutService({
    required Uri baseUrl,
    required String tenantName,
    required String email,
    required String password,
    String? initialToken,
    TotemHttpClient? httpClient,
  })  : _tenantName = tenantName,
        _email = email,
        _password = password {
    _token = initialToken?.trim().isEmpty ?? true ? null : initialToken!.trim();
    _http = httpClient ??
        TotemHttpClient(
          baseUrl: baseUrl,
          tokenProvider: () => _token,
        );
  }

  final String _tenantName;
  final String _email;
  final String _password;
  late final TotemHttpClient _http;

  String? _token;
  Map<String, String>? _skuIdByCode;

  @override
  Future<CheckoutStartResult> startCheckout({
    required List<CheckoutItem> items,
    required OrderFulfillment fulfillment,
    required PaymentMethod paymentMethod,
    String? comanda,
  }) async {
    await _getToken();
    final skuIdByCode = await _getSkuIdByCode();

    final qtyBySkuId = <String, int>{};
    for (final item in items) {
      for (final code in item.skuCodes) {
        final skuId = skuIdByCode[code.trim().toUpperCase()];
        if (skuId == null) continue;
        qtyBySkuId.update(skuId, (v) => v + item.quantity, ifAbsent: () => item.quantity);
      }
    }

    if (qtyBySkuId.isEmpty) {
      throw Exception('Nenhum SKU do carrinho possui correspondência no catálogo da API (/api/skus).');
    }

    final cartId = await _createCart();
    for (final entry in qtyBySkuId.entries) {
      await _addCartItem(cartId: cartId, skuId: entry.key, quantity: entry.value);
    }

    final resp = await _http.postJson<Map<String, dynamic>>(
      '/api/checkout',
      body: <String, Object?>{
        'cartId': cartId,
        'fulfillment': _fulfillmentToApi(fulfillment),
        'paymentMethod': _paymentMethodToApi(paymentMethod),
        if (comanda != null && comanda.trim().isNotEmpty) 'comanda': comanda.trim(),
      },
    );

    final orderId = (resp['orderId'] as String?) ?? '';
    final payment = (resp['payment'] as Map?)?.cast<String, Object?>();
    final paymentId = (payment?['id'] as String?) ?? '';
    if (orderId.isEmpty || paymentId.isEmpty) {
      throw Exception('Resposta inválida do /api/checkout.');
    }

    PixCharge? pixCharge;
    final pixPayload = payment?['pixPayload'] as String?;
    final pixExpiresAt = payment?['pixExpiresAt'] as String?;
    if (pixPayload != null && pixPayload.isNotEmpty && pixExpiresAt != null && pixExpiresAt.isNotEmpty) {
      pixCharge = PixCharge(
        amountCents: (resp['totalCents'] as num?)?.toInt() ?? 0,
        payload: pixPayload,
        expiresAt: DateTime.parse(pixExpiresAt).toLocal(),
        reference: paymentId,
      );
    }

    return CheckoutStartResult(orderId: orderId, paymentId: paymentId, pixCharge: pixCharge);
  }

  @override
  Future<PaymentResult> confirmPayment({required String paymentId}) async {
    await _getToken();
    final resp = await _http.postJson<Map<String, dynamic>>(
      '/api/checkout/payments/$paymentId/confirm',
      body: null,
    );

    final payment = (resp['payment'] as Map?)?.cast<String, Object?>();
    final status = (payment?['status'] as String?) ?? '';
    final isApproved = status == 'Approved';
    final transactionId = payment?['transactionId'] as String?;
    return PaymentResult(
      isApproved: isApproved,
      transactionId: transactionId,
      message: isApproved ? 'APROVADO' : 'NEGADO',
    );
  }

  Future<String> _getToken() async {
    final existing = _token;
    if (existing != null && existing.isNotEmpty) return existing;

    final resp = await _http.postJson<Map<String, dynamic>>(
      '/api/auth/login',
      body: <String, Object?>{
        'tenantName': _tenantName,
        'email': _email,
        'password': _password,
      },
    );
    final token = ((resp['token'] ?? resp['Token']) as String?)?.trim() ?? '';
    if (token.isEmpty) throw Exception('Falha ao autenticar no /api/auth/login.');
    _token = token;
    return token;
  }

  Future<Map<String, String>> _getSkuIdByCode() async {
    final cached = _skuIdByCode;
    if (cached != null) return cached;

    final list = await _http.getJson<List<dynamic>>('/api/skus');
    final map = <String, String>{};
    for (final raw in list) {
      if (raw is! Map) continue;
      final code = (raw['code'] as String?)?.trim().toUpperCase();
      final id = raw['id'] as String?;
      if (code == null || code.isEmpty || id == null || id.isEmpty) continue;
      map[code] = id;
    }
    _skuIdByCode = map;
    return map;
  }

  Future<String> _createCart() async {
    final resp = await _http.postJson<Map<String, dynamic>>('/api/carts', body: null);
    final id = (resp['id'] as String?) ?? '';
    if (id.isEmpty) throw Exception('Resposta inválida do /api/carts.');
    return id;
  }

  Future<void> _addCartItem({
    required String cartId,
    required String skuId,
    required int quantity,
  }) async {
    await _http.postJson<Object?>(
      '/api/carts/$cartId/items',
      body: <String, Object?>{
        'skuId': skuId,
        'quantity': quantity,
      },
    );
  }
}

String _fulfillmentToApi(OrderFulfillment v) {
  return switch (v) {
    OrderFulfillment.dineIn => 'DineIn',
    OrderFulfillment.takeAway => 'TakeAway',
  };
}

String _paymentMethodToApi(PaymentMethod v) {
  return switch (v) {
    PaymentMethod.creditCard => 'CreditCard',
    PaymentMethod.debitCard => 'DebitCard',
    PaymentMethod.pix => 'Pix',
  };
}
