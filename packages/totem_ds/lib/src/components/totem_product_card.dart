import 'package:flutter/material.dart';

class TotemProductCard extends StatelessWidget {
  const TotemProductCard({
    super.key,
    required this.title,
    required this.priceText,
    required this.badgeCount,
    required this.onBuy,
    this.onModify,
    this.onTap,
    this.description,
    this.imageUrl,
    this.buyLabel = 'Comprar',
    this.modifyLabel = 'Modificar',
  });

  final String title;
  final String priceText;
  final String? description;
  final String? imageUrl;
  final int badgeCount;
  final VoidCallback onBuy;
  final VoidCallback? onModify;
  final VoidCallback? onTap;
  final String buyLabel;
  final String modifyLabel;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return Card(
      elevation: 0,
      color: colorScheme.surfaceContainerHighest,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: ClipRRect(
                  borderRadius: BorderRadius.circular(14),
                  child: DecoratedBox(
                    decoration: BoxDecoration(
                      color: colorScheme.surface,
                      border: Border.all(color: colorScheme.outlineVariant),
                      borderRadius: BorderRadius.circular(14),
                    ),
                    child: Stack(
                      children: [
                        Positioned.fill(
                          child: imageUrl == null
                              ? Center(
                                  child: Icon(
                                    Icons.fastfood,
                                    size: 28,
                                    color: colorScheme.onSurfaceVariant,
                                  ),
                                )
                              : Image.network(
                                  imageUrl!,
                                  fit: BoxFit.cover,
                                  errorBuilder: (context, error, stackTrace) {
                                    return Center(
                                      child: Icon(
                                        Icons.fastfood,
                                        size: 28,
                                        color: colorScheme.onSurfaceVariant,
                                      ),
                                    );
                                  },
                                ),
                        ),
                        if (badgeCount > 0)
                          Positioned(
                            top: 8,
                            right: 8,
                            child: _Badge(count: badgeCount),
                          ),
                      ],
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 8),
              Text(
                title,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800),
              ),
              const SizedBox(height: 6),
              Text(
                priceText,
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.w800,
                  color: colorScheme.primary,
                ),
              ),
              if (description != null) ...[
                const SizedBox(height: 6),
                Text(
                  description!,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(color: colorScheme.onSurfaceVariant),
                ),
              ],
              const SizedBox(height: 8),
              SizedBox(
                width: double.infinity,
                child: FilledButton(
                  style: FilledButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 10),
                    textStyle: const TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  onPressed: onBuy,
                  child: Text(buyLabel),
                ),
              ),
              if (onModify != null) ...[
                const SizedBox(height: 8),
                SizedBox(
                  width: double.infinity,
                  child: OutlinedButton(
                    onPressed: onModify,
                    style: OutlinedButton.styleFrom(
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                      textStyle: const TextStyle(fontSize: 13, fontWeight: FontWeight.w800),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                    ),
                    child: Text(modifyLabel),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _Badge extends StatelessWidget {
  const _Badge({required this.count});

  final int count;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final background = count > 0 ? colorScheme.primary : colorScheme.surface;
    final foreground = count > 0 ? colorScheme.onPrimary : colorScheme.onSurface;

    return Container(
      width: 34,
      height: 34,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: colorScheme.outlineVariant),
      ),
      child: Text(
        '$count',
        style: TextStyle(fontSize: 13, fontWeight: FontWeight.w900, color: foreground),
      ),
    );
  }
}
