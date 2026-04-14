import 'package:totem/src/http/totem_http.dart';

import '../../../checkout/domain/entities/checkout_order.dart';
import '../../domain/entities/pdv_order.dart';
import '../../domain/repositories/pdv_repository.dart';

class TotemApiPdvRepository implements PdvRepository {
  TotemApiPdvRepository({
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
  Future<PdvCashRegisterShift?> getCurrentCashRegisterShift() async {
    if (_token.trim().isEmpty) throw Exception('Sessão inválida (token ausente).');

    final resp = await _http.getJson<Object?>('/api/pos/cashier/current');
    if (resp == null) return null;
    final m = (resp as Map).cast<String, Object?>();
    return _shiftFromApi(m);
  }

  @override
  Future<PdvCashRegisterShift> openCashRegisterShift({
    required int openingCashCents,
  }) async {
    if (_token.trim().isEmpty) throw Exception('Sessão inválida (token ausente).');
    if (openingCashCents < 0) throw Exception('Valor de abertura inválido.');

    final resp = await _http.postJson<Map<String, dynamic>>(
      '/api/pos/cashier/open',
      body: <String, Object?>{
        'openingCashCents': openingCashCents,
      },
    );
    return _shiftFromApi(resp);
  }

  @override
  Future<PdvCloseCashRegisterResult> closeCashRegisterShift({
    required int closingCashCents,
  }) async {
    if (_token.trim().isEmpty) throw Exception('Sessão inválida (token ausente).');
    if (closingCashCents < 0) throw Exception('Valor de fechamento inválido.');

    final resp = await _http.postJson<Map<String, dynamic>>(
      '/api/pos/cashier/close',
      body: <String, Object?>{
        'closingCashCents': closingCashCents,
      },
    );

    final shift = _shiftFromApi((resp['shift'] as Map).cast<String, Object?>());
    final paymentsRaw = (resp['payments'] as List?) ?? const [];
    final payments = paymentsRaw
        .map((e) => (e as Map).cast<String, Object?>())
        .map(
          (m) => PdvPaymentMethodSummaryItem(
            method: _paymentMethodFromApi((m['method'] as String?) ?? ''),
            amountCents: (m['amountCents'] as num?)?.toInt() ?? 0,
          ),
        )
        .toList(growable: false);

    return PdvCloseCashRegisterResult(
      shift: shift,
      totalSalesCents: (resp['totalSalesCents'] as num?)?.toInt() ?? 0,
      totalCashSalesCents: (resp['totalCashSalesCents'] as num?)?.toInt() ?? 0,
      expectedCashCents: (resp['expectedCashCents'] as num?)?.toInt() ?? 0,
      closingCashCents: (resp['closingCashCents'] as num?)?.toInt() ?? closingCashCents,
      differenceCents: (resp['differenceCents'] as num?)?.toInt() ?? 0,
      payments: payments,
    );
  }

  @override
  Future<List<PdvOrder>> listOrdersByComanda({
    required String comanda,
    required bool includePaid,
    int limit = 50,
  }) async {
    if (_token.trim().isEmpty) throw Exception('Sessão inválida (token ausente).');
    final trimmed = comanda.trim();
    if (trimmed.isEmpty) throw Exception('Informe a comanda.');

    final resp = await _http.getJson<List<dynamic>>(
      '/api/pos/orders',
      queryParameters: <String, dynamic>{
        'comanda': trimmed,
        'includePaid': includePaid,
        'limit': limit,
      },
    );

    return resp
        .map((e) => (e as Map).cast<String, Object?>())
        .map(
          (m) => PdvOrder(
            orderId: (m['orderId'] as String?) ?? '',
            comanda: (m['comanda'] as String?) ?? trimmed,
            status: (m['status'] as String?) ?? '',
            kitchenStatus: (m['kitchenStatus'] as String?) ?? '',
            totalCents: (m['totalCents'] as num?)?.toInt() ?? 0,
            createdAt: DateTime.parse((m['createdAt'] as String?) ?? DateTime.now().toIso8601String()).toLocal(),
            updatedAt: DateTime.parse((m['updatedAt'] as String?) ?? DateTime.now().toIso8601String()).toLocal(),
          ),
        )
        .where((o) => o.orderId.isNotEmpty)
        .toList(growable: false);
  }

  @override
  Future<PdvPaymentResult> payOrder({
    required String orderId,
    required PaymentMethod paymentMethod,
    String? transactionId,
  }) async {
    if (_token.trim().isEmpty) throw Exception('Sessão inválida (token ausente).');
    final trimmedOrderId = orderId.trim();
    if (trimmedOrderId.isEmpty) throw Exception('Pedido inválido.');

    final resp = await _http.postJson<Map<String, dynamic>>(
      '/api/pos/orders/$trimmedOrderId/pay',
      body: <String, Object?>{
        'paymentMethod': _paymentMethodToApi(paymentMethod),
        if (transactionId != null && transactionId.trim().isNotEmpty) 'transactionId': transactionId.trim(),
      },
    );

    final payment = (resp['payment'] as Map?)?.cast<String, Object?>();
    return PdvPaymentResult(
      orderId: (resp['orderId'] as String?) ?? trimmedOrderId,
      orderStatus: (resp['orderStatus'] as String?) ?? '',
      kitchenStatus: (resp['kitchenStatus'] as String?) ?? '',
      paymentStatus: (payment?['status'] as String?) ?? '',
      transactionId: (payment?['transactionId'] as String?) ?? '',
    );
  }
}

PdvCashRegisterShift _shiftFromApi(Map<String, Object?> m) {
  final rawStatus = (m['status'] as String?) ?? '';
  final status = switch (rawStatus) {
    'Open' => PdvCashRegisterShiftStatus.open,
    'Closed' => PdvCashRegisterShiftStatus.closed,
    _ => PdvCashRegisterShiftStatus.closed,
  };

  final openedAtRaw = (m['openedAt'] as String?) ?? DateTime.now().toIso8601String();
  final closedAtRaw = m['closedAt'] as String?;

  return PdvCashRegisterShift(
    id: (m['id'] as String?) ?? '',
    status: status,
    openedByEmail: (m['openedByEmail'] as String?) ?? '',
    openingCashCents: (m['openingCashCents'] as num?)?.toInt() ?? 0,
    openedAt: DateTime.parse(openedAtRaw).toLocal(),
    closingCashCents: (m['closingCashCents'] as num?)?.toInt(),
    totalSalesCents: (m['totalSalesCents'] as num?)?.toInt(),
    totalCashSalesCents: (m['totalCashSalesCents'] as num?)?.toInt(),
    expectedCashCents: (m['expectedCashCents'] as num?)?.toInt(),
    closedAt: closedAtRaw == null ? null : DateTime.parse(closedAtRaw).toLocal(),
  );
}

PaymentMethod _paymentMethodFromApi(String v) {
  return switch (v) {
    'CreditCard' => PaymentMethod.creditCard,
    'DebitCard' => PaymentMethod.debitCard,
    'Pix' => PaymentMethod.pix,
    'Cash' => PaymentMethod.cash,
    _ => PaymentMethod.cash,
  };
}

String _paymentMethodToApi(PaymentMethod v) {
  return switch (v) {
    PaymentMethod.creditCard => 'CreditCard',
    PaymentMethod.debitCard => 'DebitCard',
    PaymentMethod.pix => 'Pix',
    PaymentMethod.cash => 'Cash',
  };
}
