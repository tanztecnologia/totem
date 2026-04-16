import 'package:flutter/foundation.dart';
import 'package:opentelemetry/api.dart' as otel_api;
import 'package:opentelemetry/sdk.dart' as otel_sdk;

/// Inicializa e expõe o tracer global do OpenTelemetry.
///
/// Deve ser chamado em `main()` antes de `runApp()`.
/// Se nenhum endpoint OTLP for fornecido e não estiver em debug, não registra
/// nenhum provider (zero overhead em produção sem coletor configurado).
class TotemTelemetry {
  TotemTelemetry._();

  static otel_api.Tracer? _tracer;

  /// Tracer global. Retorna `null` se o OTel não foi inicializado.
  static otel_api.Tracer? get tracer => _tracer;

  /// Inicializa o SDK do OpenTelemetry.
  ///
  /// - [otlpEndpoint]: URL base do coletor OTLP/HTTP, ex: `http://localhost:4318`.
  ///   Se fornecido, traces são enviados via [otel_sdk.BatchSpanProcessor].
  /// - Em modo debug, sempre adiciona o [otel_sdk.ConsoleExporter].
  static Future<void> init({String? otlpEndpoint}) async {
    final processors = <otel_sdk.SpanProcessor>[];

    final endpoint = otlpEndpoint?.trim();
    if (endpoint != null && endpoint.isNotEmpty) {
      final exporter = otel_sdk.CollectorExporter(
        Uri.parse('$endpoint/v1/traces'),
      );
      processors.add(otel_sdk.BatchSpanProcessor(exporter));
    }

    if (kDebugMode) {
      processors.add(otel_sdk.SimpleSpanProcessor(otel_sdk.ConsoleExporter()));
    }

    if (processors.isEmpty) return;

    final provider = otel_sdk.TracerProviderBase(
      processors: processors,
      resource: otel_sdk.Resource([
        otel_api.Attribute.fromString('service.name', 'totem-flutter'),
        otel_api.Attribute.fromString('service.version', '1.0.0'),
        otel_api.Attribute.fromString(
          'deployment.environment',
          kDebugMode ? 'development' : 'production',
        ),
      ]),
    );

    otel_api.registerGlobalTracerProvider(provider);
    _tracer = otel_api.globalTracerProvider.getTracer('totem');
  }
}
