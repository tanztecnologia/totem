class CheckoutItem {
  const CheckoutItem({
    required this.id,
    required this.title,
    required this.skuIds,
    required this.quantity,
    required this.unitPriceCents,
    this.subtitle,
    this.imageUrl,
  });

  final String id;
  final String title;
  final List<String> skuIds;
  final int quantity;
  final int unitPriceCents;
  final String? subtitle;
  final String? imageUrl;
}
