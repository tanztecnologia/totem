class TotemHttpException implements Exception {
  final int? statusCode;
  final String message;
  final Object? data;

  const TotemHttpException({
    required this.message,
    this.statusCode,
    this.data,
  });

  @override
  String toString() {
    if (statusCode == null) return message;
    return 'HTTP $statusCode: $message';
  }
}
