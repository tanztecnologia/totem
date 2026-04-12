class CheckoutItem {
  const CheckoutItem({
    required this.id,
    required this.title,
    required this.skuCodes,
    required this.quantity,
    required this.unitPriceCents,
    this.subtitle,
    this.imageUrl,
  });

  final String id;
  final String title;
  final List<String> skuCodes;
  final int quantity;
  final int unitPriceCents;
  final String? subtitle;
  final String? imageUrl;
}
