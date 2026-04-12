import 'package:flutter/material.dart';

import 'totem_quantity_stepper.dart';

class TotemProductDetailSheet extends StatelessWidget {
  const TotemProductDetailSheet({
    super.key,
    required this.title,
    required this.priceText,
    required this.quantity,
    required this.onIncrement,
    required this.onDecrement,
    required this.onAdd,
    this.imageUrl,
    this.description,
    this.ingredients = const <TotemIngredientItem>[],
    this.includedIngredientIds = const <String>{},
    this.onToggleIngredient,
    this.addLabel = 'Adicionar ao pedido',
  });

  final String title;
  final String priceText;
  final String? imageUrl;
  final String? description;
  final List<TotemIngredientItem> ingredients;
  final Set<String> includedIngredientIds;
  final ValueChanged<String>? onToggleIngredient;
  final int quantity;
  final VoidCallback onIncrement;
  final VoidCallback onDecrement;
  final VoidCallback onAdd;
  final String addLabel;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return SafeArea(
      top: false,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 16, 20, 20),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                const SizedBox(width: 40),
                Expanded(
                  child: Text(
                    title,
                    textAlign: TextAlign.center,
                    style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w900),
                  ),
                ),
                SizedBox(
                  width: 40,
                  child: IconButton(
                    onPressed: () => Navigator.of(context).pop(),
                    iconSize: 20,
                    visualDensity: VisualDensity.compact,
                    padding: EdgeInsets.zero,
                    icon: const Icon(Icons.close),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            Container(
              height: 160,
              decoration: BoxDecoration(
                color: colorScheme.surfaceContainerHighest,
                borderRadius: BorderRadius.circular(18),
                border: Border.all(color: colorScheme.outlineVariant),
              ),
              clipBehavior: Clip.antiAlias,
              alignment: Alignment.center,
              child: imageUrl == null
                  ? Icon(Icons.fastfood, size: 34, color: colorScheme.onSurfaceVariant)
                  : Image.network(
                      imageUrl!,
                      fit: BoxFit.cover,
                      width: double.infinity,
                      height: double.infinity,
                      errorBuilder: (context, error, stackTrace) {
                        return Icon(Icons.fastfood, size: 34, color: colorScheme.onSurfaceVariant);
                      },
                    ),
            ),
            const SizedBox(height: 14),
            Text(
              priceText,
              textAlign: TextAlign.center,
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.w900,
                color: colorScheme.primary,
              ),
            ),
            if (description != null) ...[
              const SizedBox(height: 10),
              Text(
                description!,
                textAlign: TextAlign.center,
                style: TextStyle(color: colorScheme.onSurfaceVariant),
              ),
            ],
            if (ingredients.isNotEmpty && onToggleIngredient != null) ...[
              const SizedBox(height: 14),
              Text(
                'Ingredientes',
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w900,
                  color: colorScheme.onSurfaceVariant,
                ),
              ),
              const SizedBox(height: 10),
              Expanded(
                child: SingleChildScrollView(
                  child: Column(
                    children: [
                      for (final ingredient in ingredients)
                        _IngredientTile(
                          ingredient: ingredient,
                          isIncluded: includedIngredientIds.contains(ingredient.id),
                          onToggle: () => onToggleIngredient!(ingredient.id),
                        ),
                    ],
                  ),
                ),
              ),
            ] else ...[
              const Spacer(),
            ],
            const SizedBox(height: 16),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                TotemQuantityStepper(
                  value: quantity,
                  onIncrement: onIncrement,
                  onDecrement: onDecrement,
                  min: 1,
                ),
              ],
            ),
            const SizedBox(height: 16),
            FilledButton(
              onPressed: onAdd,
              style: FilledButton.styleFrom(
                padding: const EdgeInsets.symmetric(vertical: 16),
                textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w900),
              ),
              child: Text(addLabel),
            ),
          ],
        ),
      ),
    );
  }
}

class TotemIngredientItem {
  const TotemIngredientItem({
    required this.id,
    required this.label,
    this.isRemovable = true,
  });

  final String id;
  final String label;
  final bool isRemovable;
}

class _IngredientTile extends StatelessWidget {
  const _IngredientTile({
    required this.ingredient,
    required this.isIncluded,
    required this.onToggle,
  });

  final TotemIngredientItem ingredient;
  final bool isIncluded;
  final VoidCallback onToggle;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final canToggle = ingredient.isRemovable;

    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: InkWell(
        onTap: canToggle ? onToggle : null,
        borderRadius: BorderRadius.circular(14),
        child: DecoratedBox(
          decoration: BoxDecoration(
            color: colorScheme.surfaceContainerLow,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: colorScheme.outlineVariant),
          ),
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    ingredient.label,
                    style: const TextStyle(fontWeight: FontWeight.w800),
                  ),
                ),
                if (!ingredient.isRemovable)
                  Icon(
                    Icons.lock_outline,
                    size: 18,
                    color: colorScheme.onSurfaceVariant,
                  )
                else
                  Checkbox(
                    value: isIncluded,
                    onChanged: (_) => onToggle(),
                    visualDensity: VisualDensity.compact,
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
