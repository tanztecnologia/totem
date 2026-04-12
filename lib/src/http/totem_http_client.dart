import 'dart:async';

import 'package:dio/dio.dart';

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
          handler.next(options);
        },
        onError: (error, handler) {
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
