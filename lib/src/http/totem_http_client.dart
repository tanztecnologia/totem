import 'dart:async';
import 'dart:developer' as developer;

import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';

import '../telemetry/otel_dio_interceptor.dart';
import '../telemetry/totem_telemetry.dart';
import 'totem_http_exception.dart';

typedef TotemTokenProvider = FutureOr<String?> Function();

class TotemHttpClient {
  final Dio _dio;

  TotemHttpClient._(this._dio);

  factory TotemHttpClient({
    required Uri baseUrl,
    Duration connectTimeout = const Duration(seconds: 10),
    Duration receiveTimeout = const Duration(seconds: 30),
    TotemTokenProvider? tokenProvider,
    bool enableLogging = true,
  }) {
    final dio = Dio(
      BaseOptions(
        baseUrl: baseUrl.toString(),
        connectTimeout: connectTimeout,
        receiveTimeout: receiveTimeout,
        headers: const {
          'Accept': 'application/json',
        },
        responseType: ResponseType.json,
      ),
    );

    // Adiciona interceptor OTel antes do de logging, para que o traceparent
    // apareça nos logs e os spans HTTP sejam criados primeiro.
    final otelTracer = TotemTelemetry.tracer;
    if (otelTracer != null) {
      dio.interceptors.add(OtelDioInterceptor(otelTracer));
    }

    const maxBodyChars = 4000;

    void emitLog(String message) {
      developer.log(message, name: 'TotemHttp');
      if (kDebugMode) debugPrint(message);
    }

    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          if (tokenProvider != null) {
            final token = await tokenProvider();
            if (token != null && token.trim().isNotEmpty) {
              options.headers['Authorization'] = 'Bearer $token';
            }
          }
          if (options.data != null) {
            options.headers['Content-Type'] = 'application/json';
          }

          if (enableLogging) {
            final uri = options.uri;
            final method = options.method.toUpperCase();

            final headers = _sanitizeHeaders(options.headers);
            final data = _sanitizeBody(options.data);

            final bodyPreview = data == null ? '' : _truncate(data.toString(), maxBodyChars);
            final headersPreview = headers.isEmpty ? '' : _truncate(headers.toString(), maxBodyChars);

            final summary = '[HTTP] $method $uri';
            final details = <String>[
              if (headersPreview.isNotEmpty) 'headers=$headersPreview',
              if (bodyPreview.isNotEmpty) 'body=$bodyPreview',
            ].join(' ');

            emitLog(details.isEmpty ? summary : '$summary $details');
            options.extra['_totem_http_start_ms'] = DateTime.now().millisecondsSinceEpoch;
          }

          handler.next(options);
        },
        onResponse: (response, handler) {
          if (enableLogging) {
            final req = response.requestOptions;
            final uri = req.uri;
            final method = req.method.toUpperCase();
            final status = response.statusCode ?? 0;

            final startedMs = req.extra['_totem_http_start_ms'] as int?;
            final elapsedMs = startedMs == null ? null : DateTime.now().millisecondsSinceEpoch - startedMs;

            final data = _sanitizeBody(response.data);
            final bodyPreview = data == null ? '' : _truncate(data.toString(), maxBodyChars);

            final prefix = elapsedMs == null
                ? '[HTTP] $method $uri => $status'
                : '[HTTP] $method $uri => $status in ${elapsedMs}ms';

            emitLog(bodyPreview.isEmpty ? prefix : '$prefix body=$bodyPreview');
          }
          handler.next(response);
        },
        onError: (error, handler) {
          if (enableLogging) {
            final req = error.requestOptions;
            final uri = req.uri;
            final method = req.method.toUpperCase();
            final status = error.response?.statusCode;

            final startedMs = req.extra['_totem_http_start_ms'] as int?;
            final elapsedMs = startedMs == null ? null : DateTime.now().millisecondsSinceEpoch - startedMs;

            final data = _sanitizeBody(error.response?.data);
            final bodyPreview = data == null ? '' : _truncate(data.toString(), maxBodyChars);

            final prefix = elapsedMs == null
                ? '[HTTP] $method $uri => ERROR${status == null ? '' : ' $status'}'
                : '[HTTP] $method $uri => ERROR${status == null ? '' : ' $status'} in ${elapsedMs}ms';

            final msg = (error.message ?? '').trim();
            final full = bodyPreview.isEmpty ? '$prefix $msg'.trim() : '$prefix $msg body=$bodyPreview'.trim();
            emitLog(full);
          }
          handler.reject(error);
        },
      ),
    );

    return TotemHttpClient._(dio);
  }

  Future<T> getJson<T>(
    String path, {
    Map<String, dynamic>? queryParameters,
  }) async {
    final response = await _request<T>(
      method: 'GET',
      path: path,
      queryParameters: queryParameters,
      data: null,
    );
    return response;
  }

  Future<T> postJson<T>(
    String path, {
    Object? body,
    Map<String, dynamic>? queryParameters,
  }) async {
    final response = await _request<T>(
      method: 'POST',
      path: path,
      queryParameters: queryParameters,
      data: body,
    );
    return response;
  }

  Future<T> putJson<T>(
    String path, {
    Object? body,
    Map<String, dynamic>? queryParameters,
  }) async {
    final response = await _request<T>(
      method: 'PUT',
      path: path,
      queryParameters: queryParameters,
      data: body,
    );
    return response;
  }

  Future<void> delete(
    String path, {
    Object? body,
    Map<String, dynamic>? queryParameters,
  }) async {
    await _requestNoReturn(
      method: 'DELETE',
      path: path,
      queryParameters: queryParameters,
      data: body,
    );
  }

  Future<void> _requestNoReturn({
    required String method,
    required String path,
    required Map<String, dynamic>? queryParameters,
    required Object? data,
  }) async {
    try {
      await _dio.request<Object?>(
        path,
        data: data,
        queryParameters: queryParameters,
        options: Options(method: method),
      );
    } on DioException catch (e) {
      throw _mapDioException(e);
    } catch (e) {
      throw TotemHttpException(message: e.toString());
    }
  }

  Future<T> _request<T>({
    required String method,
    required String path,
    required Map<String, dynamic>? queryParameters,
    required Object? data,
  }) async {
    try {
      final response = await _dio.request<Object?>(
        path,
        data: data,
        queryParameters: queryParameters,
        options: Options(method: method),
      );

      final raw = response.data;
      if (raw is T) return raw;
      return raw as T;
    } on DioException catch (e) {
      throw _mapDioException(e);
    } catch (e) {
      throw TotemHttpException(message: e.toString());
    }
  }

  TotemHttpException _mapDioException(DioException e) {
    final response = e.response;
    final statusCode = response?.statusCode;
    final data = response?.data;

    final message = _extractMessage(data) ??
        e.message ??
        (e.type == DioExceptionType.connectionTimeout
            ? 'Timeout de conexão'
            : e.type == DioExceptionType.receiveTimeout
                ? 'Timeout de resposta'
                : 'Falha na requisição');

    return TotemHttpException(
      message: message,
      statusCode: statusCode,
      data: data,
    );
  }

  String? _extractMessage(Object? data) {
    if (data is Map) {
      final raw = data['error'] ?? data['message'];
      if (raw is String && raw.trim().isNotEmpty) return raw.trim();
    }
    if (data is String && data.trim().isNotEmpty) return data.trim();
    return null;
  }
}

Map<String, Object?> _sanitizeHeaders(Map<String, dynamic> headers) {
  final sanitized = <String, Object?>{};
  headers.forEach((key, value) {
    final k = key.toString();
    final lower = k.toLowerCase();
    if (lower == 'authorization') {
      sanitized[k] = 'Bearer ***';
      return;
    }
    if (lower.contains('api-key') || lower.contains('apikey')) {
      sanitized[k] = '***';
      return;
    }
    sanitized[k] = value;
  });
  return sanitized;
}

Object? _sanitizeBody(Object? body) {
  if (body is Map) {
    final out = <String, Object?>{};
    body.forEach((key, value) {
      final k = key.toString();
      final lower = k.toLowerCase();
      if (lower.contains('password') || lower.contains('token') || lower.contains('apikey') || lower.contains('api_key')) {
        out[k] = '***';
        return;
      }
      out[k] = _sanitizeBody(value);
    });
    return out;
  }
  if (body is List) {
    return body.map(_sanitizeBody).toList(growable: false);
  }
  return body;
}

String _truncate(String v, int maxChars) {
  if (v.length <= maxChars) return v;
  return '${v.substring(0, maxChars)}…';
}
