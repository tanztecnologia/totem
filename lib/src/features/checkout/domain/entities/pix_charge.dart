class PixCharge {
  const PixCharge({
    required this.amountCents,
    required this.payload,
    required this.expiresAt,
    required this.reference,
  });

  final int amountCents;
  final String payload;
  final DateTime expiresAt;
  final String reference;
}

