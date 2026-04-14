import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:totem_ds/totem_ds.dart';

import '../../../checkout/domain/entities/checkout_item.dart';
import '../../../checkout/domain/entities/checkout_order.dart';
import '../../../checkout/domain/usecases/start_checkout.dart';
import '../../../checkout/domain/services/checkout_service.dart';
import '../../../kiosk/domain/entities/product.dart';
import '../../../kiosk/presentation/bloc/kiosk_bloc.dart';

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

class WaiterPage extends StatefulWidget {
  const WaiterPage({super.key});

  @override
  State<WaiterPage> createState() => _WaiterPageState();
}

class _WaiterPageState extends State<WaiterPage> {
  final _comandaController = TextEditingController();

  @override
  void dispose() {
    _comandaController.dispose();
    super.dispose();
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

        return FractionallySizedBox(
          heightFactor: 0.8,
          child: StatefulBuilder(
            builder: (context, setState) {
              return TotemProductDetailSheet(
                title: product.name,
                priceText: product.formattedPrice,
                description: product.description,
                imageUrl: product.imageUrl,
                ingredients: ingredients,
                includedIngredientIds: included,
                onToggleIngredient: (ingredientId) {
                  setState(() {
                    final opt = optionBySkuId[ingredientId];
                    if (opt == null || !opt.isRemovable) return;
                    if (included.contains(ingredientId)) {
                      included.remove(ingredientId);
                    } else {
                      included.add(ingredientId);
                    }
                  });
                },
                quantity: qty,
                onIncrement: () => setState(() => qty++),
                onDecrement: () => setState(() {
                  if (qty > 1) qty--;
                }),
                onAdd: () {
                  final excluded = defaultIncluded.difference(included);
                  final added = included.difference(defaultIncluded);
                  kioskBloc.add(
                    KioskProductAdded(
                      product,
                      quantity: qty,
                      excludedSkuIds: excluded.toList(growable: false),
                      addedSkuIds: added.toList(growable: false),
                    ),
                  );
                  Navigator.pop(context);
                  messenger.showSnackBar(
                    SnackBar(
                      content: Text('${product.name} adicionado'),
                      duration: const Duration(milliseconds: 900),
                    ),
                  );
                },
              );
            },
          ),
        );
      },
    );
  }

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

  void _showCheckoutDialog(BuildContext context, KioskState state) {
    if (state.cartQtyByLineId.isEmpty) return;

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
            skuIds: <String>[
            product.baseSku.id,
            ...line.addedSkuIds,
          ],
            subtitle: _subtitleForLine(product, line),
            quantity: e.value,
            unitPriceCents: unitCents,
            imageUrl: product.imageUrl,
          );
        })
        .whereType<CheckoutItem>()
        .toList(growable: false);

    showDialog<void>(
      context: context,
      builder: (dialogContext) {
        bool isSubmitting = false;

        return StatefulBuilder(builder: (statefulContext, setState) {
          return AlertDialog(
            title: const Text('Finalizar Pedido'),
            content: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Text('Digite o número ou nome da comanda:'),
                const SizedBox(height: 16),
                TextField(
                  controller: _comandaController,
                  decoration: const InputDecoration(
                    labelText: 'Comanda',
                    border: OutlineInputBorder(),
                  ),
                  autofocus: true,
                ),
              ],
            ),
            actions: [
              TextButton(
                onPressed: isSubmitting ? null : () => Navigator.pop(dialogContext),
                child: const Text('Cancelar'),
              ),
              FilledButton(
                onPressed: isSubmitting
                    ? null
                    : () async {
                        final comanda = _comandaController.text.trim();
                        if (comanda.isEmpty) {
                          ScaffoldMessenger.of(context).showSnackBar(
                            const SnackBar(content: Text('Informe a comanda')),
                          );
                          return;
                        }

                        setState(() => isSubmitting = true);

                        final service = this.context.read<CheckoutService>();
                        final startCheckout = StartCheckout(service);

                        try {
                          await startCheckout(
                            items: checkoutItems,
                            fulfillment: OrderFulfillment.dineIn,
                            paymentMethod: PaymentMethod.pix,
                            comanda: comanda,
                          );

                          if (!dialogContext.mounted) return;
                          Navigator.pop(dialogContext);

                          if (!mounted) return;
                          this.context.read<KioskBloc>().add(const KioskCartCleared());
                          _comandaController.clear();

                          ScaffoldMessenger.of(this.context).showSnackBar(
                            SnackBar(
                              content: Text('Pedido enviado para a comanda $comanda!'),
                              behavior: SnackBarBehavior.fixed,
                            ),
                          );
                        } catch (e) {
                          setState(() => isSubmitting = false);
                          if (!mounted) return;
                          ScaffoldMessenger.of(this.context).showSnackBar(
                            SnackBar(
                              content: Text('Erro: $e'),
                              behavior: SnackBarBehavior.fixed,
                            ),
                          );
                        }
                      },
                child: isSubmitting
                    ? const SizedBox(
                        width: 20,
                        height: 20,
                        child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                      )
                    : const Text('Confirmar'),
              ),
            ],
          );
        });
      },
    );
  }

  void _showCart(BuildContext context, KioskState state) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (bottomSheetContext) {
        return BlocProvider.value(
          value: context.read<KioskBloc>(),
          child: BlocBuilder<KioskBloc, KioskState>(
            builder: (context, blocState) {
              final items = blocState.cartQtyByLineId.entries
                  .map((e) {
                    final line = blocState.cartLinesById[e.key];
                    if (line == null) return null;
                    final product = blocState.productById[line.productId];
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

              return FractionallySizedBox(
                heightFactor: 0.8,
                child: TotemCartPanel(
                  items: items,
                  totalText: blocState.cartTotalFormatted,
                  onClear: () {
                    context.read<KioskBloc>().add(const KioskCartCleared());
                    Navigator.pop(bottomSheetContext);
                  },
                  onCheckout: () {
                    Navigator.pop(bottomSheetContext);
                    _showCheckoutDialog(this.context, blocState);
                  },
                  onIncrement: (lineId) {
                    final line = blocState.cartLinesById[lineId];
                    if (line == null) return;
                    final product = blocState.productById[line.productId];
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
                    if (blocState.cartItemsCount <= 1) {
                      Navigator.pop(bottomSheetContext);
                    }
                  },
                ),
              );
            },
          ),
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<KioskBloc, KioskState>(builder: (context, state) {
      return Scaffold(
        appBar: AppBar(
          title: const Text('Garçom - Novo Pedido'),
          actions: [
            if (state.cartItemsCount > 0)
              ActionChip(
                onPressed: () => _showCart(context, state),
                label: Text('${state.cartItemsCount} itens'),
                avatar: const Icon(Icons.shopping_cart),
              ),
            const SizedBox(width: 16),
          ],
        ),
        body: Row(
          children: [
            // Menu lateral adaptado para mobile
            SizedBox(
              width: 90,
              child: DecoratedBox(
                decoration: BoxDecoration(
                  color: Theme.of(context).colorScheme.surfaceContainerLow,
                  border: Border(right: BorderSide(color: Theme.of(context).colorScheme.outlineVariant)),
                ),
                child: ListView.builder(
                  itemCount: state.categories.length,
                  itemBuilder: (context, index) {
                    final cat = state.categories[index];
                    final isSelected = state.selectedCategory?.id == cat.id;
                    return InkWell(
                      onTap: () => context.read<KioskBloc>().add(KioskCategorySelected(cat)),
                      child: Container(
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        color: isSelected ? Theme.of(context).colorScheme.primaryContainer : null,
                        child: Column(
                          children: [
                            Icon(_iconForCategoryId(cat.id),
                                color: isSelected ? Theme.of(context).colorScheme.primary : null),
                            const SizedBox(height: 8),
                            Text(cat.name,
                                textAlign: TextAlign.center,
                                style: TextStyle(
                                  fontSize: 12,
                                  fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
                                  color: isSelected ? Theme.of(context).colorScheme.primary : null,
                                )),
                          ],
                        ),
                      ),
                    );
                  },
                ),
              ),
            ),
            // Produtos
            Expanded(
              child: state.isLoading
                  ? const Center(child: CircularProgressIndicator())
                  : state.products.isEmpty
                      ? const Center(child: Text('Nenhum produto nesta categoria.'))
                      : ListView.builder(
                          padding: const EdgeInsets.all(16),
                          itemCount: state.products.length,
                          itemBuilder: (context, index) {
                            final product = state.products[index];
                            final bloc = context.read<KioskBloc>();

                            return Padding(
                              padding: const EdgeInsets.only(bottom: 16),
                              child: SizedBox(
                                height: 320,
                                child: TotemProductCard(
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
                                ),
                              ),
                            );
                          },
                        ),
            ),
          ],
        ),
        floatingActionButton: state.cartItemsCount > 0
            ? FloatingActionButton.extended(
                onPressed: () => _showCheckoutDialog(context, state),
                label: Text('Finalizar (${state.cartTotalFormatted})'),
                icon: const Icon(Icons.check),
              )
            : null,
      );
    });
  }
}


