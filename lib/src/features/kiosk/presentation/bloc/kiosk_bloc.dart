import 'package:bloc/bloc.dart';

import '../../domain/entities/product.dart';
import '../../domain/repositories/catalog_repository.dart';
import 'kiosk_event.dart';
import 'kiosk_state.dart';

export 'kiosk_event.dart';
export 'kiosk_state.dart';

class KioskBloc extends Bloc<KioskEvent, KioskState> {
  KioskBloc({required CatalogRepository catalogRepository})
      : _catalogRepository = catalogRepository,
        super(KioskState.initial()) {
    on<KioskLoadRequested>(_onLoadRequested);
    on<KioskCategorySelected>(_onCategorySelected);
    on<KioskProductAdded>(_onProductAdded);
    on<KioskCartLineDecremented>(_onCartLineDecremented);
    on<KioskCartCleared>(_onCartCleared);
  }

  final CatalogRepository _catalogRepository;

  Future<void> _onLoadRequested(
    KioskLoadRequested event,
    Emitter<KioskState> emit,
  ) async {
    emit(state.copyWith(isLoading: true));

    final categories = await _catalogRepository.getCategories();
    final selected = state.selectedCategory ?? (categories.isNotEmpty ? categories.first : null);

    if (selected != null) {
      final products = await _catalogRepository.getProductsForCategory(selected.id);
      final productById = Map<String, Product>.from(state.productById);
      for (final product in products) {
        productById[product.id] = product;
      }
      emit(
        state.copyWith(
          isLoading: false,
          categories: categories,
          selectedCategory: selected,
          products: products,
          productById: productById,
        ),
      );
    } else {
      emit(
        state.copyWith(
          isLoading: false,
          categories: categories,
          selectedCategory: null,
          products: const <Product>[],
        ),
      );
    }
  }

  Future<void> _onCategorySelected(
    KioskCategorySelected event,
    Emitter<KioskState> emit,
  ) async {
    final category = event.category;
    if (state.selectedCategory?.id == category.id) return;

    emit(state.copyWith(isLoading: true, selectedCategory: category));
    final products = await _catalogRepository.getProductsForCategory(category.id);
    final productById = Map<String, Product>.from(state.productById);
    for (final product in products) {
      productById[product.id] = product;
    }
    emit(state.copyWith(isLoading: false, products: products, productById: productById));
  }

  void _onProductAdded(
    KioskProductAdded event,
    Emitter<KioskState> emit,
  ) {
    if (event.quantity <= 0) return;

    final productById = Map<String, Product>.from(state.productById);
    productById[event.product.id] = event.product;

    final lineId = CartLine.buildId(event.product.id, event.excludedSkuIds, event.addedSkuIds);
    final cartLinesById = Map<String, CartLine>.from(state.cartLinesById);
    cartLinesById[lineId] = CartLine(
      id: lineId,
      productId: event.product.id,
      excludedSkuIds: List<String>.from(event.excludedSkuIds),
      addedSkuIds: List<String>.from(event.addedSkuIds),
    );

    final current = state.cartQtyByLineId[lineId] ?? 0;
    final cartQtyByLineId = Map<String, int>.from(state.cartQtyByLineId);
    cartQtyByLineId[lineId] = current + event.quantity;

    emit(
      state.copyWith(
        productById: productById,
        cartLinesById: cartLinesById,
        cartQtyByLineId: cartQtyByLineId,
      ),
    );
  }

  void _onCartLineDecremented(
    KioskCartLineDecremented event,
    Emitter<KioskState> emit,
  ) {
    if (event.quantity <= 0) return;

    final current = state.cartQtyByLineId[event.lineId] ?? 0;
    if (current <= 0) return;

    final cartQtyByLineId = Map<String, int>.from(state.cartQtyByLineId);
    final next = current - event.quantity;
    if (next > 0) {
      cartQtyByLineId[event.lineId] = next;
    } else {
      cartQtyByLineId.remove(event.lineId);
    }

    emit(state.copyWith(cartQtyByLineId: cartQtyByLineId));
  }

  void _onCartCleared(
    KioskCartCleared event,
    Emitter<KioskState> emit,
  ) {
    if (state.cartQtyByLineId.isEmpty) return;
    emit(
      state.copyWith(
        cartQtyByLineId: const <String, int>{},
        cartLinesById: const <String, CartLine>{},
      ),
    );
  }
}
