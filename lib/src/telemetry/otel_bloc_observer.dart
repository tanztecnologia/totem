import 'package:bloc/bloc.dart';
import 'package:opentelemetry/api.dart' as otel_api;

/// BlocObserver que emite spans OTel para eventos críticos de negócio
/// e erros em qualquer BLoC/Cubit.
///
/// Eventos rastreados:
/// - Checkout: [CheckoutStarted], [CheckoutConfirmed], [CheckoutPixPaymentConfirmed]
/// - Kiosk: [KioskLoadRequested], [KioskCategorySelected], [KioskCartCleared]
class OtelBlocObserver extends BlocObserver {
  OtelBlocObserver(this._tracer);

  final otel_api.Tracer _tracer;

  static const _trackedEvents = {
    'CheckoutStarted',
    'CheckoutConfirmed',
    'CheckoutPixPaymentConfirmed',
    'KioskLoadRequested',
    'KioskCategorySelected',
    'KioskCartCleared',
  };

  @override
  void onEvent(Bloc bloc, Object? event) {
    super.onEvent(bloc, event);
    final eventName = event?.runtimeType.toString() ?? 'Unknown';
    if (!_trackedEvents.contains(eventName)) return;

    // Span instantâneo — marca o momento do evento no trace.
    // A latência real é capturada pelos spans HTTP filhos (OtelDioInterceptor).
    final span = _tracer.startSpan(
      'bloc.$eventName',
      kind: otel_api.SpanKind.internal,
      attributes: [
        otel_api.Attribute.fromString('bloc.type', bloc.runtimeType.toString()),
        otel_api.Attribute.fromString('bloc.event', eventName),
      ],
    );
    span.end();
  }

  @override
  void onError(BlocBase bloc, Object error, StackTrace stackTrace) {
    super.onError(bloc, error, stackTrace);
    final span = _tracer.startSpan(
      'bloc.error',
      kind: otel_api.SpanKind.internal,
      attributes: [
        otel_api.Attribute.fromString('bloc.type', bloc.runtimeType.toString()),
        otel_api.Attribute.fromString('error.type', error.runtimeType.toString()),
      ],
    );
    span.setStatus(otel_api.StatusCode.error, error.toString());
    span.recordException(error, stackTrace: stackTrace);
    span.end();
  }
}
