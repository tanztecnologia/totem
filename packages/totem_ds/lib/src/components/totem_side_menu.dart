import 'package:flutter/material.dart';

class TotemSideMenu<T> extends StatelessWidget {
  const TotemSideMenu({
    super.key,
    required this.items,
    required this.selectedId,
    required this.idOf,
    required this.labelOf,
    required this.onSelect,
    this.leadingBuilder,
  });

  final List<T> items;
  final String? selectedId;
  final String Function(T item) idOf;
  final String Function(T item) labelOf;
  final ValueChanged<T> onSelect;
  final Widget? Function(T item)? leadingBuilder;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return ListView.separated(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
      itemBuilder: (context, index) {
        final item = items[index];
        final id = idOf(item);
        final isSelected = id == selectedId;
        final leading = leadingBuilder?.call(item);

        return SizedBox(
          height: 56,
          child: FilledButton.tonal(
            style: FilledButton.styleFrom(
              backgroundColor:
                  isSelected ? colorScheme.primaryContainer : colorScheme.surface,
              foregroundColor:
                  isSelected ? colorScheme.onPrimaryContainer : colorScheme.onSurface,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              textStyle: const TextStyle(fontSize: 18, fontWeight: FontWeight.w600),
              alignment: Alignment.centerLeft,
              padding: const EdgeInsets.symmetric(horizontal: 16),
            ),
            onPressed: () => onSelect(item),
            child: Row(
              children: [
                if (leading != null) ...[
                  IconTheme(
                    data: const IconThemeData(size: 20),
                    child: leading,
                  ),
                  const SizedBox(width: 12),
                ],
                Expanded(
                  child: Text(
                    labelOf(item),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
            ),
          ),
        );
      },
      separatorBuilder: (context, index) => const SizedBox(height: 10),
      itemCount: items.length,
    );
  }
}
