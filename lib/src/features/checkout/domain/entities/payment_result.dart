class PaymentResult {
  const PaymentResult({
    required this.isApproved,
    this.transactionId,
    this.message,
  });

  final bool isApproved;
  final String? transactionId;
  final String? message;
}

