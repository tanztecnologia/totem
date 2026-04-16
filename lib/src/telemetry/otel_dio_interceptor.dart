import 'package:dio/dio.dart';
import 'package:opentelemetry/api.dart' as otel_api;

/// Interceptor do Dio que:
/// - Cria um span OTel por requisição HTTP (kind = client).
/// - Injeta o header W3C `traceparent` para propagação distribuída.
/// - Registra `http.status_code` na resposta e marca o span com erro se >= 400.
class OtelDioInterceptor extends Interceptor {
  OtelDioInterceptor(this._tracer);

  final otel_api.Tracer _tracer;
  final _propagator = otel_api.W3CTraceContextPropagator();
  final _setter = _DioHeaderSetter();

  static const String _spanKey = '_otel_span';

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    final method = options.method.toUpperCase();
    final span = _tracer.startSpan(
      'HTTP $method',
      kind: otel_api.SpanKind.client,
      attributes: [
        otel_api.Attribute.fromString('http.method', method),
        otel_api.Attribute.fromString('http.url', options.uri.toString()),
        otel_api.Attribute.fromString('net.peer.name', options.uri.host),
        otel_api.Attribute.fromInt('net.peer.port', options.uri.port),
      ],
    );

    // Injeta traceparent para correlação com o backend
    final spanContext = otel_api.contextWithSpan(otel_api.Context.current, span);
    _propagator.inject(spanContext, options.headers, _setter);

    options.extra[_spanKey] = span;
    handler.next(options);
  }

  @override
  void onResponse(Response response, ResponseInterceptorHandler handler) {
    final span = response.requestOptions.extra[_spanKey] as otel_api.Span?;
    if (span != null) {
      final status = response.statusCode ?? 0;
      span.setAttribute(otel_api.Attribute.fromInt('http.status_code', status));
      span.setStatus(status >= 400 ? otel_api.StatusCode.error : otel_api.StatusCode.ok);
      span.end();
    }
    handler.next(response);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    final span = err.requestOptions.extra[_spanKey] as otel_api.Span?;
    if (span != null) {
      final statusCode = err.response?.statusCode;
      if (statusCode != null) {
        span.setAttribute(otel_api.Attribute.fromInt('http.status_code', statusCode));
      }
      span.setStatus(otel_api.StatusCode.error, err.message ?? 'HTTP Error');
      span.recordException(err, stackTrace: err.stackTrace);
      span.end();
    }
    handler.reject(err);
  }
}

class _DioHeaderSetter implements otel_api.TextMapSetter<Map<String, dynamic>> {
  @override
  void set(Map<String, dynamic> carrier, String key, String value) {
    carrier[key] = value;
  }
}
