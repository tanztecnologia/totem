part of 'dashboard_page.dart';

class _CreateSkuContent extends StatelessWidget {
  const _CreateSkuContent({
    required this.state,
  });

  final DashboardState state;

  @override
  Widget build(BuildContext context) {
    return _CreateSkuForm(
      title: 'Cadastrar SKU',
      subtitle: 'O código do SKU é gerado automaticamente (00001, 00002...).',
      state: state,
    );
  }
}

class _FindSkuContent extends StatefulWidget {
  const _FindSkuContent({
    required this.state,
  });

  final DashboardState state;

  @override
  State<_FindSkuContent> createState() => _FindSkuContentState();
}

class _FindSkuContentState extends State<_FindSkuContent> {
  late final TextEditingController _codeController;
  bool _isLoading = false;
  CatalogAdminSkuResult? _result;

  @override
  void initState() {
    super.initState();
    _codeController = TextEditingController();
  }

  @override
  void dispose() {
    _codeController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _DateRangeHeader(from: widget.state.fromInclusive, to: widget.state.toInclusive),
        const SizedBox(height: 12),
        Card(
          clipBehavior: Clip.antiAlias,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Buscar SKU', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w900)),
                const SizedBox(height: 12),
                TextField(
                  enabled: !_isLoading,
                  controller: _codeController,
                  decoration: const InputDecoration(
                    labelText: 'Código do SKU',
                    hintText: 'ex.: drinks-COCA',
                    border: OutlineInputBorder(),
                  ),
                  onSubmitted: (_) => _search(context),
                ),
                const SizedBox(height: 12),
                FilledButton(
                  onPressed: _isLoading ? null : () => _search(context),
                  child: _isLoading
                      ? const SizedBox(
                          height: 18,
                          width: 18,
                          child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                        )
                      : const Text('Buscar'),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),
        if (_result != null) _SkuDetailsCard(result: _result!),
      ],
    );
  }

  Future<void> _search(BuildContext context) async {
    final repo = context.read<CatalogAdminRepository>();
    final code = _codeController.text.trim();
    if (code.isEmpty) {
      _showSnack(context, 'Informe o código do SKU.');
      return;
    }

    setState(() {
      _isLoading = true;
      _result = null;
    });
    try {
      final result = await repo.getSkuByCode(code: code);
      if (!context.mounted) return;
      if (result == null) {
        _showSnack(context, 'SKU não encontrado.');
        setState(() => _isLoading = false);
        return;
      }
      setState(() {
        _isLoading = false;
        _result = result;
      });
    } catch (e) {
      if (!context.mounted) return;
      _showSnack(context, e.toString());
      setState(() => _isLoading = false);
    }
  }
}

class _SkuDetailsCard extends StatelessWidget {
  const _SkuDetailsCard({required this.result});

  final CatalogAdminSkuResult result;

  @override
  Widget build(BuildContext context) {
    return Card(
      clipBehavior: Clip.antiAlias,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Detalhes', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w900)),
            const SizedBox(height: 10),
            _PreviewRow(label: 'ID', value: result.id),
            const SizedBox(height: 6),
            _PreviewRow(label: 'Categoria (código)', value: result.categoryCode),
            const SizedBox(height: 6),
            _PreviewRow(label: 'Código', value: result.code),
            const SizedBox(height: 6),
            _PreviewRow(label: 'Nome', value: result.name),
            const SizedBox(height: 6),
            _PreviewRow(label: 'Preço', value: _formatMoney(result.priceCents)),
            const SizedBox(height: 6),
            _PreviewRow(label: 'Ativo', value: result.isActive ? 'Sim' : 'Não'),
            if (result.averagePrepSeconds != null) ...[
              const SizedBox(height: 6),
              _PreviewRow(label: 'Preparo (s)', value: result.averagePrepSeconds.toString()),
            ],
            if (result.imageUrl != null && result.imageUrl!.trim().isNotEmpty) ...[
              const SizedBox(height: 6),
              _PreviewRow(label: 'Imagem', value: result.imageUrl!),
            ],
          ],
        ),
      ),
    );
  }
}

class _ProductsContent extends StatefulWidget {
  const _ProductsContent({
    required this.state,
  });

  final DashboardState state;

  @override
  State<_ProductsContent> createState() => _ProductsContentState();
}

class _ProductsContentState extends State<_ProductsContent> {
  late final TextEditingController _queryController;

  bool _includeInactive = true;
  bool _isLoading = false;
  bool _isLoadingMore = false;

  Map<String, String> _categoryNameByCode = const <String, String>{};
  List<CatalogAdminSkuResult> _items = const [];
  String? _nextCursorCode;
  String? _nextCursorId;

  bool get _hasMore => _nextCursorCode != null && _nextCursorId != null && _nextCursorId!.trim().isNotEmpty;

  @override
  void initState() {
    super.initState();
    _queryController = TextEditingController();
    Future<void>.microtask(() async {
      if (!mounted) return;
      await _loadCategories();
      await _search(reset: true);
    });
  }

  @override
  void dispose() {
    _queryController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _DateRangeHeader(from: widget.state.fromInclusive, to: widget.state.toInclusive),
        const SizedBox(height: 12),
        Card(
          clipBehavior: Clip.antiAlias,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Produtos', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w900)),
                const SizedBox(height: 12),
                TextField(
                  enabled: !_isLoading,
                  controller: _queryController,
                  decoration: const InputDecoration(
                    labelText: 'Buscar',
                    hintText: 'Código ou nome',
                    border: OutlineInputBorder(),
                  ),
                  onSubmitted: (_) => _search(reset: true),
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Switch(
                      value: _includeInactive,
                      onChanged: _isLoading ? null : (v) => setState(() => _includeInactive = v),
                    ),
                    const SizedBox(width: 8),
                    const Expanded(child: Text('Incluir inativos')),
                    FilledButton(
                      onPressed: _isLoading ? null : () => _search(reset: true),
                      child: _isLoading
                          ? const SizedBox(
                              height: 18,
                              width: 18,
                              child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                            )
                          : const Text('Buscar'),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),
        if (_items.isEmpty && _isLoading)
          const Center(
            child: Padding(
              padding: EdgeInsets.all(24),
              child: CircularProgressIndicator(),
            ),
          )
        else if (_items.isEmpty)
          const Padding(
            padding: EdgeInsets.symmetric(vertical: 8),
            child: Text('Nenhum produto encontrado.'),
          )
        else
          Card(
            clipBehavior: Clip.antiAlias,
            child: Column(
              children: [
                ListView.separated(
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  itemCount: _items.length,
                  separatorBuilder: (_, __) => const Divider(height: 1),
                  itemBuilder: (context, index) {
                    final sku = _items[index];
                    final categoryCode = sku.categoryCode;
                    final categoryName = categoryCode.isEmpty ? null : _categoryNameByCode[categoryCode];
                    final categoryLabel =
                        categoryCode.isEmpty ? null : (categoryName == null || categoryName.trim().isEmpty ? categoryCode : categoryName);
                    return ListTile(
                      leading: categoryCode.isEmpty
                          ? null
                          : Chip(
                              label: Text(categoryLabel ?? categoryCode),
                            ),
                      title: Text(sku.name.isEmpty ? sku.code : sku.name),
                      subtitle: Text(
                        'Categoria: ${categoryLabel ?? '-'} • ${sku.code} • ${_formatMoney(sku.priceCents)}',
                      ),
                      trailing: sku.isActive ? const Icon(Icons.check_circle_outline) : const Icon(Icons.do_not_disturb_on_outlined),
                      onTap: () => _openEditSku(context, sku),
                    );
                  },
                ),
                if (_hasMore) ...[
                  const Divider(height: 1),
                  Padding(
                    padding: const EdgeInsets.all(12),
                    child: FilledButton(
                      onPressed: _isLoadingMore ? null : _loadMore,
                      child: _isLoadingMore
                          ? const SizedBox(
                              height: 18,
                              width: 18,
                              child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                            )
                          : const Text('Carregar mais'),
                    ),
                  ),
                ],
              ],
            ),
          ),
      ],
    );
  }

  Future<void> _search({required bool reset}) async {
    final repo = context.read<CatalogAdminRepository>();
    if (_isLoading) return;

    if (reset) {
      setState(() {
        _isLoading = true;
        _items = const [];
        _nextCursorCode = null;
        _nextCursorId = null;
      });
    } else {
      setState(() => _isLoading = true);
    }

    try {
      final page = await repo.searchSkus(
        query: _queryController.text.trim(),
        limit: 50,
        cursorCode: null,
        cursorId: null,
        includeInactive: _includeInactive,
      );
      if (!mounted) return;
      setState(() {
        _isLoading = false;
        _items = page.items;
        _nextCursorCode = page.nextCursorCode;
        _nextCursorId = page.nextCursorId;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => _isLoading = false);
      _showSnack(context, e.toString());
    }
  }

  Future<void> _loadCategories() async {
    final repo = context.read<CatalogAdminRepository>();
    try {
      final list = await repo.listCategories(includeInactive: true);
      if (!mounted) return;
      setState(() {
        _categoryNameByCode = <String, String>{
          for (final c in list) c.code: c.name,
        };
      });
    } catch (_) {}
  }

  Future<void> _loadMore() async {
    final repo = context.read<CatalogAdminRepository>();
    if (_isLoadingMore || !_hasMore) return;

    setState(() => _isLoadingMore = true);
    try {
      final page = await repo.searchSkus(
        query: _queryController.text.trim(),
        limit: 50,
        cursorCode: _nextCursorCode,
        cursorId: _nextCursorId,
        includeInactive: _includeInactive,
      );
      if (!mounted) return;
      setState(() {
        _isLoadingMore = false;
        _items = [..._items, ...page.items];
        _nextCursorCode = page.nextCursorCode;
        _nextCursorId = page.nextCursorId;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() => _isLoadingMore = false);
      _showSnack(context, e.toString());
    }
  }

  Future<void> _openEditSku(BuildContext context, CatalogAdminSkuResult sku) async {
    final repo = context.read<CatalogAdminRepository>();
    final updated = await showDialog<CatalogAdminSkuResult?>(
      context: context,
      builder: (context) => _EditSkuDialog(repo: repo, sku: sku),
    );
    if (!mounted) return;
    if (updated == null) return;
    setState(() {
      _items = _items.map((x) => x.id == updated.id ? updated : x).toList(growable: false);
    });
  }
}

class _EditSkuDialog extends StatefulWidget {
  const _EditSkuDialog({
    required this.repo,
    required this.sku,
  });

  final CatalogAdminRepository repo;
  final CatalogAdminSkuResult sku;

  @override
  State<_EditSkuDialog> createState() => _EditSkuDialogState();
}

class _EditSkuDialogState extends State<_EditSkuDialog> {
  late final TextEditingController _categoryCodeController;
  late final TextEditingController _nameController;
  late final TextEditingController _priceController;
  late final TextEditingController _prepSecondsController;
  late final TextEditingController _imageUrlController;
  late final TextEditingController _stockOnHandBaseQtyController;
  late final TextEditingController _stockEntryQtyController;
  late final TextEditingController _consumeSourceSkuCodeController;
  late final TextEditingController _consumeQtyController;

  late CatalogAdminSkuResult _sku;
  int? _stockBaseUnit;
  String _stockEntryUnit = 'g';
  String _consumeUnit = 'g';
  List<CatalogAdminSkuStockConsumption> _consumptions = const [];

  bool _isActive = true;
  bool _isSaving = false;
  bool _isStockBusy = false;

  @override
  void initState() {
    super.initState();
    _sku = widget.sku;
    _categoryCodeController = TextEditingController(text: _sku.categoryCode);
    _nameController = TextEditingController(text: _sku.name);
    _priceController = TextEditingController(text: (_sku.priceCents / 100).toStringAsFixed(2).replaceAll('.', ','));
    _prepSecondsController = TextEditingController(text: _sku.averagePrepSeconds?.toString() ?? '');
    _imageUrlController = TextEditingController(text: _sku.imageUrl ?? '');
    _stockOnHandBaseQtyController = TextEditingController(text: _sku.stockOnHandBaseQty?.toString() ?? '');
    _stockEntryQtyController = TextEditingController();
    _consumeSourceSkuCodeController = TextEditingController();
    _consumeQtyController = TextEditingController();
    _stockBaseUnit = _sku.stockBaseUnit;
    final entryUnits = _entryUnitOptions();
    if (!entryUnits.contains(_stockEntryUnit)) _stockEntryUnit = entryUnits.first;
    _isActive = _sku.isActive;
    Future<void>.microtask(_loadConsumptions);
  }

  @override
  void dispose() {
    _categoryCodeController.dispose();
    _nameController.dispose();
    _priceController.dispose();
    _prepSecondsController.dispose();
    _imageUrlController.dispose();
    _stockOnHandBaseQtyController.dispose();
    _stockEntryQtyController.dispose();
    _consumeSourceSkuCodeController.dispose();
    _consumeQtyController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final dialogMaxHeight = (MediaQuery.of(context).size.height * 0.8).clamp(420.0, 720.0).toDouble();
    return AlertDialog(
      title: const Text('Editar produto'),
      content: ConstrainedBox(
        constraints: const BoxConstraints(maxWidth: 560),
        child: DefaultTabController(
          length: 2,
          child: SizedBox(
            height: dialogMaxHeight,
            child: Column(
              children: [
                _PreviewRow(label: 'Código', value: _sku.code),
                const SizedBox(height: 12),
                const TabBar(
                  tabs: [
                    Tab(text: 'Produto'),
                    Tab(text: 'Impostos'),
                  ],
                ),
                const SizedBox(height: 12),
                Expanded(
                  child: TabBarView(
                    children: [
                      _buildProdutoTab(context),
                      _buildImpostosTab(),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: _isSaving ? null : () => Navigator.of(context).pop(null),
          child: const Text('Cancelar'),
        ),
        FilledButton(
          onPressed: _isSaving ? null : () => _save(context),
          child: _isSaving
              ? const SizedBox(
                  height: 18,
                  width: 18,
                  child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                )
              : const Text('Salvar'),
        ),
      ],
    );
  }

  Widget _buildProdutoTab(BuildContext context) {
    return SingleChildScrollView(
      child: Column(
        children: [
          TextField(
            enabled: !_isSaving,
            controller: _categoryCodeController,
            decoration: const InputDecoration(
              labelText: 'Categoria (código)',
              hintText: 'ex.: 00001',
              border: OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 12),
          TextField(
            enabled: !_isSaving,
            controller: _nameController,
            decoration: const InputDecoration(
              labelText: 'Nome',
              border: OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 12),
          TextField(
            enabled: !_isSaving,
            controller: _priceController,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: const InputDecoration(
              labelText: 'Preço (R\$)',
              border: OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 12),
          TextField(
            enabled: !_isSaving,
            controller: _prepSecondsController,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(
              labelText: 'Preparo (s) (opcional)',
              border: OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 12),
          TextField(
            enabled: !_isSaving,
            controller: _imageUrlController,
            decoration: const InputDecoration(
              labelText: 'Imagem (URL) (opcional)',
              border: OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Switch(value: _isActive, onChanged: _isSaving ? null : (v) => setState(() => _isActive = v)),
              const SizedBox(width: 8),
              const Text('Ativo'),
            ],
          ),
          const SizedBox(height: 12),
          Align(
            alignment: Alignment.centerLeft,
            child: Text('Estoque', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w900)),
          ),
          const SizedBox(height: 8),
          DropdownButtonFormField<int?>(
            value: _stockBaseUnit,
            items: const [
              DropdownMenuItem(value: null, child: Text('Sem controle')),
              DropdownMenuItem(value: 0, child: Text('Unidade')),
              DropdownMenuItem(value: 1, child: Text('Peso (g)')),
              DropdownMenuItem(value: 2, child: Text('Volume (ml)')),
            ],
            onChanged: _isSaving
                ? null
                : (v) => setState(() {
                      _stockBaseUnit = v;
                      final entryUnits = _entryUnitOptions();
                      if (!entryUnits.contains(_stockEntryUnit)) _stockEntryUnit = entryUnits.first;
                    }),
            decoration: const InputDecoration(
              labelText: 'Unidade base do estoque',
              border: OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 12),
          TextField(
            enabled: !_isSaving,
            controller: _stockOnHandBaseQtyController,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: const InputDecoration(
              labelText: 'Estoque atual (na unidade base)',
              border: OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: TextField(
                  enabled: !_isStockBusy,
                  controller: _stockEntryQtyController,
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  decoration: const InputDecoration(
                    labelText: 'Entrada',
                    border: OutlineInputBorder(),
                  ),
                ),
              ),
              const SizedBox(width: 10),
              SizedBox(
                width: 110,
                child: DropdownButtonFormField<String>(
                  value: _stockEntryUnit,
                  items: _entryUnitOptions().map((u) => DropdownMenuItem(value: u, child: Text(u))).toList(growable: false),
                  onChanged: _isStockBusy ? null : (v) => setState(() => _stockEntryUnit = v ?? _stockEntryUnit),
                  decoration: const InputDecoration(border: OutlineInputBorder()),
                ),
              ),
              const SizedBox(width: 10),
              FilledButton(
                onPressed: _isStockBusy ? null : () => _addStockEntry(context),
                child: _isStockBusy
                    ? const SizedBox(height: 18, width: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                    : const Text('Adicionar'),
              ),
            ],
          ),
          const SizedBox(height: 16),
          Align(
            alignment: Alignment.centerLeft,
            child: Text('Consumo por venda', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w900)),
          ),
          const SizedBox(height: 8),
          TextField(
            enabled: !_isStockBusy,
            controller: _consumeSourceSkuCodeController,
            decoration: const InputDecoration(
              labelText: 'SKU base (código)',
              hintText: 'ex.: 00010 (batata)',
              border: OutlineInputBorder(),
            ),
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: TextField(
                  enabled: !_isStockBusy,
                  controller: _consumeQtyController,
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  decoration: const InputDecoration(
                    labelText: 'Qtd por venda',
                    border: OutlineInputBorder(),
                  ),
                ),
              ),
              const SizedBox(width: 10),
              SizedBox(
                width: 110,
                child: DropdownButtonFormField<String>(
                  value: _consumeUnit,
                  items: const [
                    DropdownMenuItem(value: 'un', child: Text('un')),
                    DropdownMenuItem(value: 'g', child: Text('g')),
                    DropdownMenuItem(value: 'kg', child: Text('kg')),
                    DropdownMenuItem(value: 'ml', child: Text('ml')),
                    DropdownMenuItem(value: 'l', child: Text('l')),
                  ],
                  onChanged: _isStockBusy ? null : (v) => setState(() => _consumeUnit = v ?? _consumeUnit),
                  decoration: const InputDecoration(border: OutlineInputBorder()),
                ),
              ),
              const SizedBox(width: 10),
              FilledButton.tonal(
                onPressed: _isStockBusy ? null : () => _saveConsumption(context),
                child: const Text('Salvar'),
              ),
            ],
          ),
          if (_consumptions.isNotEmpty) ...[
            const SizedBox(height: 12),
            ..._consumptions.map(
              (c) => Padding(
                padding: const EdgeInsets.only(bottom: 6),
                child: _PreviewRow(
                  label: 'Consome',
                  value: c.sourceSkuCode.trim().isEmpty ? '${c.sourceSkuId} • ${c.quantityBase}' : '${c.sourceSkuCode} • ${c.quantityBase}',
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildImpostosTab() {
    return SingleChildScrollView(
      child: Column(
        children: [
          _PreviewRow(label: 'cProd', value: _sku.nfeCProd ?? '-'),
          const SizedBox(height: 6),
          _PreviewRow(label: 'cEAN', value: _sku.nfeCEan ?? '-'),
          const SizedBox(height: 6),
          _PreviewRow(label: 'CFOP', value: _sku.nfeCfop ?? '-'),
          const SizedBox(height: 6),
          _PreviewRow(label: 'ICMS orig', value: _sku.nfeIcmsOrig ?? '-'),
          const SizedBox(height: 6),
          _PreviewRow(label: 'ICMS CST', value: _sku.nfeIcmsCst ?? '-'),
          const SizedBox(height: 6),
          _PreviewRow(label: 'ICMS modBC', value: _sku.nfeIcmsModBc ?? '-'),
          const SizedBox(height: 6),
          _PreviewRow(label: 'vBC', value: _formatNum(_sku.nfeIcmsVBc)),
          const SizedBox(height: 6),
          _PreviewRow(label: 'pICMS', value: _formatNum(_sku.nfeIcmsPIcms)),
          const SizedBox(height: 6),
          _PreviewRow(label: 'vICMS', value: _formatNum(_sku.nfeIcmsVIcms)),
          const SizedBox(height: 12),
          _PreviewRow(label: 'PIS CST', value: _sku.nfePisCst ?? '-'),
          const SizedBox(height: 6),
          _PreviewRow(label: 'PIS vBC', value: _formatNum(_sku.nfePisVBc)),
          const SizedBox(height: 6),
          _PreviewRow(label: 'PIS pPIS', value: _formatNum(_sku.nfePisPPis)),
          const SizedBox(height: 6),
          _PreviewRow(label: 'PIS vPIS', value: _formatNum(_sku.nfePisVPis)),
          const SizedBox(height: 12),
          _PreviewRow(label: 'COFINS CST', value: _sku.nfeCofinsCst ?? '-'),
          const SizedBox(height: 6),
          _PreviewRow(label: 'COFINS vBC', value: _formatNum(_sku.nfeCofinsVBc)),
          const SizedBox(height: 6),
          _PreviewRow(label: 'COFINS pCOFINS', value: _formatNum(_sku.nfeCofinsPCofins)),
          const SizedBox(height: 6),
          _PreviewRow(label: 'COFINS vCOFINS', value: _formatNum(_sku.nfeCofinsVCofins)),
        ],
      ),
    );
  }

  List<String> _entryUnitOptions() {
    final base = _stockBaseUnit;
    if (base == 0) return const ['un'];
    if (base == 1) return const ['g', 'kg'];
    if (base == 2) return const ['ml', 'l'];
    return const ['g', 'kg', 'ml', 'l', 'un'];
  }

  Future<void> _loadConsumptions() async {
    try {
      final list = await widget.repo.listSkuStockConsumptions(id: _sku.id);
      if (!mounted) return;
      setState(() => _consumptions = list);
    } catch (_) {}
  }

  Future<void> _addStockEntry(BuildContext context) async {
    final qty = _parseNum(_stockEntryQtyController.text);
    if (qty == null || qty <= 0) {
      _showSnack(context, 'Entrada inválida.');
      return;
    }
    final unit = _stockEntryUnit.trim();
    if (unit.isEmpty) {
      _showSnack(context, 'Unidade inválida.');
      return;
    }

    setState(() => _isStockBusy = true);
    try {
      final updated = await widget.repo.addSkuStockEntry(id: _sku.id, quantity: qty, unit: unit);
      if (!mounted) return;
      setState(() {
        _sku = updated;
        _stockOnHandBaseQtyController.text = updated.stockOnHandBaseQty?.toString() ?? '';
        _stockEntryQtyController.clear();
      });
    } catch (e) {
      if (!mounted) return;
      _showSnack(context, e.toString());
    } finally {
      if (mounted) setState(() => _isStockBusy = false);
    }
  }

  Future<void> _saveConsumption(BuildContext context) async {
    final sourceCode = _consumeSourceSkuCodeController.text.trim();
    final qty = _parseNum(_consumeQtyController.text);
    if (sourceCode.isEmpty) {
      _showSnack(context, 'Informe o SKU base.');
      return;
    }
    if (qty == null || qty <= 0) {
      _showSnack(context, 'Qtd por venda inválida.');
      return;
    }
    final unit = _consumeUnit.trim();
    if (unit.isEmpty) {
      _showSnack(context, 'Unidade inválida.');
      return;
    }

    setState(() => _isStockBusy = true);
    try {
      final list = await widget.repo.replaceSkuStockConsumptions(
        id: _sku.id,
        items: [(sourceSkuCode: sourceCode, quantity: qty, unit: unit)],
      );
      if (!mounted) return;
      setState(() => _consumptions = list);
    } catch (e) {
      if (!mounted) return;
      _showSnack(context, e.toString());
    } finally {
      if (mounted) setState(() => _isStockBusy = false);
    }
  }

  Future<void> _save(BuildContext context) async {
    final categoryCode = _categoryCodeController.text.trim();
    final name = _nameController.text.trim();
    final priceCents = _parseMoneyToCents(_priceController.text);
    final prepSeconds = int.tryParse(_prepSecondsController.text.trim());
    final imageUrl = _imageUrlController.text.trim().isEmpty ? null : _imageUrlController.text.trim();
    final stockOnHand = _parseNum(_stockOnHandBaseQtyController.text);

    if (categoryCode.isEmpty) {
      _showSnack(context, 'Código da Categoria inválido.');
      return;
    }
    if (name.length < 2) {
      _showSnack(context, 'Nome inválido.');
      return;
    }
    if (priceCents == null) {
      _showSnack(context, 'Preço inválido. Ex: 12,90');
      return;
    }
    if (_prepSecondsController.text.trim().isNotEmpty && (prepSeconds == null || prepSeconds <= 0)) {
      _showSnack(context, 'Tempo de preparo inválido.');
      return;
    }

    setState(() => _isSaving = true);
    try {
      final updated = await widget.repo.updateSku(
        id: widget.sku.id,
        categoryCode: categoryCode,
        name: name,
        priceCents: priceCents,
        averagePrepSeconds: prepSeconds,
        imageUrl: imageUrl,
        stockBaseUnit: _stockBaseUnit,
        stockOnHandBaseQty: stockOnHand,
        isActive: _isActive,
      );
      if (!context.mounted) return;
      Navigator.of(context).pop(updated);
    } catch (e) {
      if (!context.mounted) return;
      setState(() => _isSaving = false);
      _showSnack(context, e.toString());
    }
  }
}

num? _parseNum(String raw) {
  var s = raw.trim();
  if (s.isEmpty) return null;
  s = s.replaceAll(',', '.');
  return num.tryParse(s);
}

String _formatNum(num? n) {
  if (n == null) return '-';
  final s = n.toString();
  return s;
}

class _CreateSkuForm extends StatefulWidget {
  const _CreateSkuForm({
    required this.title,
    required this.subtitle,
    required this.state,
  });

  final String title;
  final String subtitle;
  final DashboardState state;

  @override
  State<_CreateSkuForm> createState() => _CreateSkuFormState();
}

class _CreateSkuFormState extends State<_CreateSkuForm> {
  late final TextEditingController _categoryCodeController;
  late final TextEditingController _nameController;
  late final TextEditingController _priceController;
  late final TextEditingController _prepSecondsController;
  late final TextEditingController _imageUrlController;
  late final TextEditingController _stockOnHandBaseQtyController;

  int? _stockBaseUnit;
  bool _isActive = true;
  bool _isSubmitting = false;

  @override
  void initState() {
    super.initState();
    _categoryCodeController = TextEditingController();
    _nameController = TextEditingController();
    _priceController = TextEditingController();
    _prepSecondsController = TextEditingController();
    _imageUrlController = TextEditingController();
    _stockOnHandBaseQtyController = TextEditingController();
  }

  @override
  void dispose() {
    _categoryCodeController.dispose();
    _nameController.dispose();
    _priceController.dispose();
    _prepSecondsController.dispose();
    _imageUrlController.dispose();
    _stockOnHandBaseQtyController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _DateRangeHeader(from: widget.state.fromInclusive, to: widget.state.toInclusive),
        const SizedBox(height: 12),
        Card(
          clipBehavior: Clip.antiAlias,
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(widget.title, style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w900)),
                const SizedBox(height: 8),
                Text(widget.subtitle),
                const SizedBox(height: 16),
                TextField(
                  enabled: !_isSubmitting,
                  controller: _categoryCodeController,
                  decoration: const InputDecoration(
                    labelText: 'Categoria (código)',
                    hintText: 'ex.: 00001',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  enabled: !_isSubmitting,
                  controller: _nameController,
                  decoration: const InputDecoration(
                    labelText: 'Nome',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  enabled: !_isSubmitting,
                  controller: _priceController,
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  decoration: const InputDecoration(
                    labelText: 'Preço (R\$)',
                    hintText: 'ex.: 12,90',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  enabled: !_isSubmitting,
                  controller: _prepSecondsController,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(
                    labelText: 'Tempo médio de preparo (segundos) (opcional)',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  enabled: !_isSubmitting,
                  controller: _imageUrlController,
                  decoration: const InputDecoration(
                    labelText: 'Imagem (URL) (opcional)',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
                DropdownButtonFormField<int?>(
                  value: _stockBaseUnit,
                  items: const [
                    DropdownMenuItem(value: null, child: Text('Sem controle de estoque')),
                    DropdownMenuItem(value: 0, child: Text('Unidade')),
                    DropdownMenuItem(value: 1, child: Text('Peso (g)')),
                    DropdownMenuItem(value: 2, child: Text('Volume (ml)')),
                  ],
                  onChanged: _isSubmitting ? null : (v) => setState(() => _stockBaseUnit = v),
                  decoration: const InputDecoration(
                    labelText: 'Unidade base do estoque',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  enabled: !_isSubmitting,
                  controller: _stockOnHandBaseQtyController,
                  keyboardType: const TextInputType.numberWithOptions(decimal: true),
                  decoration: const InputDecoration(
                    labelText: 'Estoque inicial (na unidade base) (opcional)',
                    border: OutlineInputBorder(),
                  ),
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Switch(
                      value: _isActive,
                      onChanged: _isSubmitting ? null : (v) => setState(() => _isActive = v),
                    ),
                    const SizedBox(width: 8),
                    const Text('Ativo'),
                  ],
                ),
                const SizedBox(height: 16),
                FilledButton(
                  onPressed: _isSubmitting ? null : () => _submit(context),
                  child: _isSubmitting
                      ? const SizedBox(
                          height: 18,
                          width: 18,
                          child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white),
                        )
                      : const Text('Salvar'),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Future<void> _submit(BuildContext context) async {
    final repo = context.read<CatalogAdminRepository>();

    final categoryCode = _categoryCodeController.text.trim();
    final name = _nameController.text.trim();
    final priceCents = _parseMoneyToCents(_priceController.text);
    final prepSeconds = int.tryParse(_prepSecondsController.text.trim());
    final imageUrl = _imageUrlController.text.trim().isEmpty ? null : _imageUrlController.text.trim();
    final stockOnHand = _parseNum(_stockOnHandBaseQtyController.text);

    if (categoryCode.isEmpty) {
      _showSnack(context, 'Informe o código da Categoria.');
      return;
    }
    if (name.isEmpty) {
      _showSnack(context, 'Informe o nome.');
      return;
    }
    if (priceCents == null) {
      _showSnack(context, 'Preço inválido. Ex: 12,90');
      return;
    }
    if (_prepSecondsController.text.trim().isNotEmpty && (prepSeconds == null || prepSeconds <= 0)) {
      _showSnack(context, 'Tempo de preparo inválido.');
      return;
    }

    setState(() => _isSubmitting = true);
    try {
      final created = await repo.createSku(
        categoryCode: categoryCode,
        name: name,
        priceCents: priceCents,
        averagePrepSeconds: prepSeconds,
        imageUrl: imageUrl,
        stockBaseUnit: _stockBaseUnit,
        stockOnHandBaseQty: stockOnHand,
        isActive: _isActive,
      );
      if (!context.mounted) return;
      _showSnack(context, 'SKU criado: ${created.code}');

      final configured = await showDialog<CatalogAdminSkuResult?>(
        context: context,
        builder: (context) => _EditSkuDialog(repo: repo, sku: created),
      );
      if (!context.mounted) return;
      if (configured != null) {
        _showSnack(context, 'SKU configurado: ${configured.code}');
      }

      _categoryCodeController.clear();
      _nameController.clear();
      _priceController.clear();
      _prepSecondsController.clear();
      _imageUrlController.clear();
      _stockOnHandBaseQtyController.clear();
      setState(() => _stockBaseUnit = null);
      setState(() {});
    } catch (e) {
      if (!context.mounted) return;
      _showSnack(context, e.toString());
    } finally {
      if (mounted) setState(() => _isSubmitting = false);
    }
  }
}
