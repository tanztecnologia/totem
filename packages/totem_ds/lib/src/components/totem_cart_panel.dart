import 'package:flutter/material.dart';

class TotemCartItemData {
  const TotemCartItemData({
    required this.id,
    required this.title,
    required this.unitPriceText,
    required this.quantity,
    this.imageUrl,
    this.subtitle,
  });

  final String id;
  final String title;
  final String? subtitle;
  final String unitPriceText;
  final int quantity;
  final String? imageUrl;
}

class TotemCartPanel extends StatelessWidget {
  const TotemCartPanel({
    super.key,
    required this.items,
    required this.totalText,
    required this.onClear,
    required this.onCheckout,
    required this.onIncrement,
    required this.onDecrement,
  });

  final List<TotemCartItemData> items;
  final String totalText;
  final VoidCallback onClear;
  final VoidCallback onCheckout;
  final ValueChanged<String> onIncrement;
  final ValueChanged<String> onDecrement;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final isEmpty = items.isEmpty;

    return DecoratedBox(
      decoration: BoxDecoration(
        color: colorScheme.surfaceContainerLow,
        border: Border(left: BorderSide(color: colorScheme.outlineVariant)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(18, 18, 18, 12),
            child: Row(
              children: [
                const Expanded(
                  child: Text(
                    'Seu pedido',
                    style: TextStyle(fontSize: 20, fontWeight: FontWeight.w900),
                  ),
                ),
                TextButton(
                  onPressed: isEmpty ? null : onClear,
                  child: const Text('Limpar'),
                ),
              ],
            ),
          ),
          Expanded(
            child: isEmpty
                ? Padding(
                    padding: const EdgeInsets.all(18),
                    child: Text(
                      'Adicione itens para começar',
                      style: TextStyle(color: colorScheme.onSurfaceVariant),
                    ),
                  )
                : ListView.separated(
                    padding: const EdgeInsets.fromLTRB(12, 0, 12, 12),
                    itemBuilder: (context, index) {
                      final item = items[index];
                      return _CartItemTile(
                        item: item,
                        onIncrement: () => onIncrement(item.id),
                        onDecrement: () => onDecrement(item.id),
                      );
                    },
                    separatorBuilder: (context, index) => const SizedBox(height: 10),
                    itemCount: items.length,
                  ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(18, 12, 18, 18),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Row(
                  children: [
                    Text(
                      'Total',
                      style: TextStyle(color: colorScheme.onSurfaceVariant),
                    ),
                    const Spacer(),
                    Text(
                      totalText,
                      style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w900),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                FilledButton(
                  onPressed: isEmpty ? null : onCheckout,
                  style: FilledButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800),
                  ),
                  child: const Text('Finalizar pedido'),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _CartItemTile extends StatelessWidget {
  const _CartItemTile({
    required this.item,
    required this.onIncrement,
    required this.onDecrement,
  });

  final TotemCartItemData item;
  final VoidCallback onIncrement;
  final VoidCallback onDecrement;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return DecoratedBox(
      decoration: BoxDecoration(
        color: colorScheme.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: colorScheme.outlineVariant),
      ),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Row(
          children: [
            if (item.imageUrl != null) ...[
              ClipRRect(
                borderRadius: BorderRadius.circular(12),
                child: Image.network(
                  item.imageUrl!,
                  width: 44,
                  height: 44,
                  fit: BoxFit.cover,
                  errorBuilder: (context, error, stackTrace) {
                    return const SizedBox(width: 44, height: 44);
                  },
                ),
              ),
              const SizedBox(width: 12),
            ],
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    item.title,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontWeight: FontWeight.w800),
                  ),
                  if (item.subtitle != null) ...[
                    const SizedBox(height: 2),
                    Text(
                      item.subtitle!,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        fontSize: 12,
                        fontWeight: FontWeight.w700,
                        color: colorScheme.onSurfaceVariant,
                      ),
                    ),
                  ],
                  const SizedBox(height: 2),
                  Text(
                    item.unitPriceText,
                    style: TextStyle(color: colorScheme.onSurfaceVariant),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 8),
            IconButton(
              onPressed: onDecrement,
              iconSize: 18,
              visualDensity: VisualDensity.compact,
              constraints: const BoxConstraints.tightFor(width: 36, height: 36),
              padding: EdgeInsets.zero,
              icon: Icon(item.quantity > 1 ? Icons.remove : Icons.delete_outline),
            ),
            Text(
              '${item.quantity}',
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w900),
            ),
            IconButton(
              onPressed: onIncrement,
              iconSize: 18,
              visualDensity: VisualDensity.compact,
              constraints: const BoxConstraints.tightFor(width: 36, height: 36),
              padding: EdgeInsets.zero,
              icon: const Icon(Icons.add),
            ),
          ],
        ),
      ),
    );
  }
}
