import 'package:totem/src/http/totem_http.dart';

import '../../../checkout/domain/entities/checkout_order.dart';
import '../../domain/entities/dashboard_order.dart';
import '../../domain/entities/dashboard_orders_page.dart';
import '../../domain/entities/dashboard_overview.dart';
import '../../domain/repositories/dashboard_repository.dart';

class TotemApiDashboardRepository implements DashboardRepository {
  TotemApiDashboardRepository({
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
  Future<DashboardOverview> getOverview({
    DateTime? fromInclusive,
    DateTime? toInclusive,
  }) async {
    if (_token.trim().isEmpty) throw Exception('Sessão inválida (token ausente).');

    final resp = await _http.getJson<Map<String, dynamic>>(
      '/api/dashboard/overview',
      queryParameters: <String, dynamic>{
        if (fromInclusive != null) 'from': fromInclusive.toUtc().toIso8601String(),
        if (toInclusive != null) 'to': toInclusive.toUtc().toIso8601String(),
      },
    );

    final paymentsRaw = (resp['paymentsByMethod'] as List?) ?? const [];
    final providersRaw = (resp['paymentsByProvider'] as List?) ?? const [];
    final kitchenRaw = (resp['ordersByKitchenStatus'] as List?) ?? const [];

    return DashboardOverview(
      fromInclusive: DateTime.parse(resp['fromInclusive']?.toString() ?? DateTime.now().toIso8601String()).toLocal(),
      toInclusive: DateTime.parse(resp['toInclusive']?.toString() ?? DateTime.now().toIso8601String()).toLocal(),
      ordersCount: (resp['ordersCount'] as num?)?.toInt() ?? 0,
      paidOrdersCount: (resp['paidOrdersCount'] as num?)?.toInt() ?? 0,
      cancelledOrdersCount: (resp['cancelledOrdersCount'] as num?)?.toInt() ?? 0,
      revenueCents: (resp['revenueCents'] as num?)?.toInt() ?? 0,
      averageTicketCents: (resp['averageTicketCents'] as num?)?.toInt() ?? 0,
      paymentsByMethod: paymentsRaw
          .map((e) => (e as Map).cast<String, Object?>())
          .map(
            (m) => DashboardPaymentMethodSummaryItem(
              method: _paymentMethodFromApi(m['method']?.toString() ?? ''),
              amountCents: (m['amountCents'] as num?)?.toInt() ?? 0,
              paymentsCount: (m['paymentsCount'] as num?)?.toInt() ?? 0,
            ),
          )
          .toList(growable: false),
      paymentsByProvider: providersRaw
          .map((e) => (e as Map).cast<String, Object?>())
          .map(
            (m) => DashboardPaymentProviderSummaryItem(
              provider: m['provider']?.toString() ?? '',
              amountCents: (m['amountCents'] as num?)?.toInt() ?? 0,
              paymentsCount: (m['paymentsCount'] as num?)?.toInt() ?? 0,
            ),
          )
          .where((x) => x.provider.trim().isNotEmpty)
          .toList(growable: false),
      ordersByKitchenStatus: kitchenRaw
          .map((e) => (e as Map).cast<String, Object?>())
          .map(
            (m) => DashboardKitchenStatusSummaryItem(
              kitchenStatus: m['kitchenStatus']?.toString() ?? '',
              ordersCount: (m['ordersCount'] as num?)?.toInt() ?? 0,
            ),
          )
          .toList(growable: false),
    );
  }

  @override
  Future<DashboardOrdersPage> listOrdersPage({
    int limit = 50,
    DateTime? cursorUpdatedAt,
    String? cursorOrderId,
  }) async {
    if (_token.trim().isEmpty) throw Exception('Sessão inválida (token ausente).');

    final resp = await _http.getJson<Map<String, dynamic>>(
      '/api/dashboard/orders',
      queryParameters: <String, dynamic>{
        'limit': limit,
        if (cursorUpdatedAt != null) 'cursorUpdatedAt': cursorUpdatedAt.toUtc().toIso8601String(),
        if (cursorOrderId != null && cursorOrderId.trim().isNotEmpty) 'cursorOrderId': cursorOrderId.trim(),
      },
    );

    final itemsRaw = (resp['items'] as List?) ?? const [];
    final items = itemsRaw
        .map((e) => (e as Map).cast<String, Object?>())
        .map(
          (m) => DashboardOrder(
            orderId: m['orderId']?.toString() ?? '',
            comanda: m['comanda']?.toString(),
            status: m['status']?.toString() ?? '',
            kitchenStatus: m['kitchenStatus']?.toString() ?? '',
            totalCents: (m['totalCents'] as num?)?.toInt() ?? 0,
            createdAt: DateTime.parse(m['createdAt']?.toString() ?? DateTime.now().toIso8601String()).toLocal(),
            updatedAt: DateTime.parse(m['updatedAt']?.toString() ?? DateTime.now().toIso8601String()).toLocal(),
            paymentStatus: m['paymentStatus']?.toString(),
            paymentMethod: _paymentMethodFromApiNullable(m['paymentMethod']?.toString()),
            paymentAmountCents: (m['paymentAmountCents'] as num?)?.toInt(),
            paymentProvider: m['paymentProvider']?.toString(),
          ),
        )
        .where((o) => o.orderId.trim().isNotEmpty)
        .toList(growable: false);

    final nextCursorUpdatedAtRaw = resp['nextCursorUpdatedAt']?.toString();
    final nextCursorUpdatedAt = nextCursorUpdatedAtRaw == null ? null : DateTime.tryParse(nextCursorUpdatedAtRaw)?.toLocal();
    final nextCursorOrderId = resp['nextCursorOrderId']?.toString();

    return DashboardOrdersPage(
      items: items,
      nextCursorUpdatedAt: nextCursorUpdatedAt,
      nextCursorOrderId: nextCursorOrderId,
    );
  }
}

PaymentMethod _paymentMethodFromApi(String v) {
  return _paymentMethodFromApiNullable(v) ?? PaymentMethod.cash;
}

PaymentMethod? _paymentMethodFromApiNullable(String? v) {
  return switch ((v ?? '').trim()) {
    'CreditCard' => PaymentMethod.creditCard,
    'DebitCard' => PaymentMethod.debitCard,
    'Pix' => PaymentMethod.pix,
    'Cash' => PaymentMethod.cash,
    _ => null,
  };
}
