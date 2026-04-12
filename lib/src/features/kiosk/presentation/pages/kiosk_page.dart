import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter/material.dart';
import 'package:totem_ds/totem_ds.dart';

import '../bloc/kiosk_bloc.dart';
import '../../../checkout/domain/entities/checkout_item.dart';
import '../../../checkout/domain/services/checkout_service.dart';
import '../../../checkout/presentation/pages/checkout_dialog.dart';
import '../../domain/entities/product.dart';

IconData _iconForCategoryId(String categoryId) {
  switch (categoryId) {
    case 'drinks':
      return Icons.local_drink_outlined;
    case 'snacks':
      return Icons.lunch_dining_outlined;
    case 'meals':
      return Icons.restaurant_outlined;
    case 'desserts':
      return Icons.icecream_outlined;
    default:
      return Icons.menu;
  }
}

class KioskPage extends StatelessWidget {
  const KioskPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: BlocBuilder<KioskBloc, KioskState>(builder: (context, state) {
        return Column(
          children: [
            TotemTopBar(
              title: 'Faça seu pedido',
              subtitle: 'Toque no item para detalhes ou use “Adicionar” para rápido',
              trailing: _OrderPill(itemsCount: state.cartItemsCount, totalText: state.cartTotalFormatted),
            ),
            Expanded(
              child: SafeArea(
                top: false,
                child: Row(
                  children: [
                    SizedBox(
                      width: 280,
                      child: DecoratedBox(
                        decoration: BoxDecoration(
                          color: Theme.of(context).colorScheme.surfaceContainerLow,
                          border: Border(
                            right:
                                BorderSide(color: Theme.of(context).colorScheme.outlineVariant),
                          ),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            const SizedBox(height: 12),
                            const Padding(
                              padding: EdgeInsets.symmetric(horizontal: 16),
                              child: Text(
                                'Categorias',
                                style: TextStyle(fontSize: 18, fontWeight: FontWeight.w900),
                              ),
                            ),
                            const SizedBox(height: 10),
                            Expanded(
                              child: TotemSideMenu(
                                items: state.categories,
                                selectedId: state.selectedCategory?.id,
                                idOf: (c) => c.id,
                                labelOf: (c) => c.name,
                                leadingBuilder: (c) => Icon(_iconForCategoryId(c.id)),
                                onSelect: (category) => context
                                    .read<KioskBloc>()
                                    .add(KioskCategorySelected(category)),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                    Expanded(
                      child: Padding(
                        padding: const EdgeInsets.fromLTRB(20, 16, 20, 16),
                        child: _CatalogPane(state: state),
                      ),
                    ),
                    SizedBox(
                      width: 360,
                      child: _CartPane(state: state),
                    ),
                  ],
                ),
              ),
            ),
          ],
        );
      }),
    );
  }
}

class _CatalogPane extends StatelessWidget {
  const _CatalogPane({required this.state});

  final KioskState state;

  @override
  Widget build(BuildContext context) {
    final selected = state.selectedCategory;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          selected?.name ?? 'Sem categoria',
          style: const TextStyle(fontSize: 26, fontWeight: FontWeight.w900),
        ),
        const SizedBox(height: 4),
        Text(
          'Escolha seu item',
          style: TextStyle(color: Theme.of(context).colorScheme.onSurfaceVariant),
        ),
        const SizedBox(height: 14),
        Expanded(
          child: state.isLoading
              ? const Center(child: CircularProgressIndicator())
              : LayoutBuilder(
                  builder: (context, constraints) {
                    final width = constraints.maxWidth;
                    final crossAxisCount = width >= 1100
                        ? 4
                        : width >= 840
                            ? 3
                            : width >= 520
                                ? 2
                                : 1;

                    return GridView.builder(
                      padding: const EdgeInsets.only(bottom: 12),
                      gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                        crossAxisCount: crossAxisCount,
                        childAspectRatio: 1.22,
                        crossAxisSpacing: 16,
                        mainAxisSpacing: 16,
                      ),
                      itemCount: state.products.length,
                      itemBuilder: (context, index) {
                        final product = state.products[index];
                        final bloc = context.read<KioskBloc>();

                        return TotemProductCard(
                          title: product.name,
                          priceText: product.formattedPrice,
                          description: product.description,
                          imageUrl: product.imageUrl,
                          badgeCount: state.cartQtyFor(product),
                          buyLabel: 'Adicionar',
                          onModify: () => _showProductDetails(context, product),
                          onTap: () => _showProductDetails(context, product),
                          onBuy: () {
                            bloc.add(KioskProductAdded(product));
                            ScaffoldMessenger.of(context).showSnackBar(
                              SnackBar(
                                content: Text('${product.name} adicionado'),
                                duration: const Duration(milliseconds: 900),
                              ),
                            );
                          },
                        );
                      },
                    );
                  },
                ),
        ),
      ],
    );
  }
}

class _CartPane extends StatelessWidget {
  const _CartPane({required this.state});

  final KioskState state;

  String _formatMoney(int cents) {
    final value = cents / 100;
    return 'R\$ ${value.toStringAsFixed(2).replaceAll('.', ',')}';
  }

  String? _subtitleForLine(Product product, CartLine line) {
    final skuById = product.skuById;
    final removedNames = line.excludedSkuIds
        .map((id) => skuById[id]?.name)
        .whereType<String>()
        .toList(growable: false)
      ..sort();
    final addedNames = line.addedSkuIds
        .map((id) => skuById[id]?.name)
        .whereType<String>()
        .toList(growable: false)
      ..sort();

    final parts = <String>[];
    if (removedNames.isNotEmpty) parts.add('Sem ${removedNames.join(', ')}');
    if (addedNames.isNotEmpty) parts.add('+ ${addedNames.join(', ')}');
    if (parts.isEmpty) return null;
    return parts.join(' • ');
  }

  @override
  Widget build(BuildContext context) {
    final items = state.cartQtyByLineId.entries
        .map((e) {
          final line = state.cartLinesById[e.key];
          if (line == null) return null;
          final product = state.productById[line.productId];
          if (product == null) return null;
          var extrasCents = 0;
          final skuById = product.skuById;
          for (final skuId in line.addedSkuIds) {
            extrasCents += skuById[skuId]?.priceCents ?? 0;
          }
          final unitCents = product.priceCents + extrasCents;
          return TotemCartItemData(
            id: e.key,
            title: product.name,
            subtitle: _subtitleForLine(product, line),
            unitPriceText: _formatMoney(unitCents),
            quantity: e.value,
            imageUrl: product.imageUrl,
          );
        })
        .whereType<TotemCartItemData>()
        .toList(growable: false)
      ..sort((a, b) => a.title.compareTo(b.title));

    return TotemCartPanel(
      items: items,
      totalText: state.cartTotalFormatted,
      onClear: () => context.read<KioskBloc>().add(const KioskCartCleared()),
      onCheckout: () => _showCheckout(context, state),
      onIncrement: (lineId) {
        final line = state.cartLinesById[lineId];
        if (line == null) return;
        final product = state.productById[line.productId];
        if (product == null) return;
        context.read<KioskBloc>().add(
              KioskProductAdded(
                product,
                excludedSkuIds: line.excludedSkuIds,
                addedSkuIds: line.addedSkuIds,
              ),
            );
      },
      onDecrement: (lineId) {
        context.read<KioskBloc>().add(KioskCartLineDecremented(lineId));
      },
    );
  }
}

class _OrderPill extends StatelessWidget {
  const _OrderPill({required this.itemsCount, required this.totalText});

  final int itemsCount;
  final String totalText;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;

    return DecoratedBox(
      decoration: BoxDecoration(
        color: colorScheme.surface,
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: colorScheme.outlineVariant),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              '$itemsCount itens',
              style: const TextStyle(fontWeight: FontWeight.w800),
            ),
            const SizedBox(width: 10),
            Text(
              totalText,
              style: TextStyle(fontWeight: FontWeight.w900, color: colorScheme.primary),
            ),
          ],
        ),
      ),
    );
  }
}

void _showProductDetails(BuildContext context, Product product) {
  final kioskBloc = context.read<KioskBloc>();
  final messenger = ScaffoldMessenger.of(context);

  showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    showDragHandle: true,
    builder: (context) {
      var qty = 1;
      String formatMoney(int cents) {
        final value = cents / 100;
        return 'R\$ ${value.toStringAsFixed(2).replaceAll('.', ',')}';
      }

      final allOptions = <ProductOption>[
        for (final g in product.optionGroups) ...g.options,
      ];
      final defaultIncluded = <String>{
        for (final o in allOptions)
          if (o.isIncludedByDefault || !o.isRemovable) o.sku.id,
      };
      final included = Set<String>.from(defaultIncluded);
      final optionBySkuId = <String, ProductOption>{
        for (final o in allOptions) o.sku.id: o,
      };

      final ingredients = allOptions
          .map((o) {
            final hasPrice = o.sku.priceCents > 0;
            final label = hasPrice ? '${o.sku.name} (+ ${formatMoney(o.sku.priceCents)})' : o.sku.name;
            return TotemIngredientItem(
              id: o.sku.id,
              label: label,
              isRemovable: o.isRemovable,
            );
          })
          .toList(growable: false);

      return StatefulBuilder(
        builder: (context, setState) {
          return TotemProductDetailSheet(
            title: product.name,
            priceText: product.formattedPrice,
            description: product.description,
            imageUrl: product.imageUrl,
            ingredients: ingredients,
            includedIngredientIds: included,
            onToggleIngredient: (ingredientId) {
              final option = optionBySkuId[ingredientId];
              if (option == null) return;
              if (!option.isRemovable) return;
              setState(() {
                if (included.contains(ingredientId)) {
                  included.remove(ingredientId);
                } else {
                  included.add(ingredientId);
                }
              });
            },
            quantity: qty,
            onIncrement: () => setState(() => qty += 1),
            onDecrement: () => setState(() => qty = qty > 1 ? qty - 1 : 1),
            onAdd: () {
              final excluded = defaultIncluded.where((id) => !included.contains(id)).toList(growable: false);
              final added = included.where((id) => !defaultIncluded.contains(id)).toList(growable: false);
              kioskBloc.add(
                    KioskProductAdded(
                      product,
                      quantity: qty,
                      excludedSkuIds: excluded,
                      addedSkuIds: added,
                    ),
                  );
              Navigator.of(context).pop();
              messenger.showSnackBar(
                SnackBar(
                  content: Text('${product.name} adicionado ($qty)'),
                  duration: const Duration(milliseconds: 900),
                ),
              );
            },
          );
        },
      );
    },
  );
}

void _showCheckout(
  BuildContext context,
  KioskState state,
) {
  String? subtitleForLine(Product product, CartLine line) {
    final skuById = product.skuById;
    final removedNames = line.excludedSkuIds
        .map((id) => skuById[id]?.name)
        .whereType<String>()
        .toList(growable: false)
      ..sort();
    final addedNames = line.addedSkuIds
        .map((id) => skuById[id]?.name)
        .whereType<String>()
        .toList(growable: false)
      ..sort();

    final parts = <String>[];
    if (removedNames.isNotEmpty) parts.add('Sem ${removedNames.join(', ')}');
    if (addedNames.isNotEmpty) parts.add('+ ${addedNames.join(', ')}');
    if (parts.isEmpty) return null;
    return parts.join(' • ');
  }

  final checkoutItems = state.cartQtyByLineId.entries
      .map((e) {
        final line = state.cartLinesById[e.key];
        if (line == null) return null;
        final product = state.productById[line.productId];
        if (product == null) return null;
        var extrasCents = 0;
        final skuById = product.skuById;
        for (final skuId in line.addedSkuIds) {
          extrasCents += skuById[skuId]?.priceCents ?? 0;
        }
        final unitCents = product.priceCents + extrasCents;
        return CheckoutItem(
          id: e.key,
          title: product.name,
          skuCodes: <String>[
            product.baseSku.id,
            ...line.addedSkuIds,
          ],
          subtitle: subtitleForLine(product, line),
          quantity: e.value,
          unitPriceCents: unitCents,
          imageUrl: product.imageUrl,
        );
      })
      .whereType<CheckoutItem>()
      .toList(growable: false)
    ..sort((a, b) => a.title.compareTo(b.title));

  final checkoutService = context.read<CheckoutService>();
  final kioskBloc = context.read<KioskBloc>();
  final messenger = ScaffoldMessenger.of(context);

  showDialog<void>(
    context: context,
    builder: (context) {
      return CheckoutDialog(
        items: checkoutItems,
        totalCents: state.cartTotalCents,
        totalText: state.cartTotalFormatted,
        checkoutService: checkoutService,
        onSuccess: () {
          kioskBloc.add(const KioskCartCleared());
          messenger.showSnackBar(
            const SnackBar(
              content: Text('Pedido finalizado'),
              duration: Duration(milliseconds: 1200),
            ),
          );
        },
      );
    },
  );
}
