import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter/services.dart';
import 'package:totem_ds/totem_ds.dart';

import 'src/features/checkout/data/repositories/in_memory_order_repository.dart';
import 'src/features/checkout/data/services/fake_tef_payment_service.dart';
import 'src/features/checkout/domain/repositories/order_repository.dart';
import 'src/features/checkout/domain/services/payment_service.dart';
import 'src/features/kiosk/data/repositories/in_memory_catalog_repository.dart';
import 'src/features/kiosk/domain/repositories/catalog_repository.dart';
import 'src/features/kiosk/presentation/bloc/kiosk_bloc.dart';
import 'src/features/kiosk/presentation/pages/kiosk_page.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);
  runApp(const TotemApp());
}

class TotemApp extends StatelessWidget {
  const TotemApp({super.key});

  @override
  Widget build(BuildContext context) {
    final kioskHome = BlocProvider(
      create: (context) => KioskBloc(
        catalogRepository: context.read<CatalogRepository>(),
      )..add(const KioskLoadRequested()),
      child: const KioskPage(),
    );

    return MultiRepositoryProvider(
      providers: [
        RepositoryProvider<CatalogRepository>(
          create: (_) => const InMemoryCatalogRepository(),
        ),
        RepositoryProvider<OrderRepository>(
          create: (_) => InMemoryOrderRepository(),
        ),
        RepositoryProvider<PaymentService>(
          create: (_) => const FakeTefPaymentService(),
        ),
      ],
      child: MaterialApp(
        title: 'TZTotem',
        theme: TotemTheme.light(),
        home: _SplashGate(child: kioskHome),
      ),
    );
  }
}

class _SplashGate extends StatefulWidget {
  const _SplashGate({
    required this.child,
  });

  final Widget child;

  @override
  State<_SplashGate> createState() => _SplashGateState();
}

class _SplashGateState extends State<_SplashGate> {
  bool _ready = false;

  @override
  void initState() {
    super.initState();
    Future<void>.delayed(const Duration(seconds: 2)).then((_) {
      if (!mounted) return;
      setState(() => _ready = true);
    });
  }

  @override
  Widget build(BuildContext context) {
    if (_ready) return widget.child;

    final colorScheme = Theme.of(context).colorScheme;
    return Scaffold(
      backgroundColor: colorScheme.surface,
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              'TZTotem',
              style: Theme.of(context).textTheme.displaySmall?.copyWith(
                    fontWeight: FontWeight.w900,
                    letterSpacing: 0.6,
                  ),
            ),
            const SizedBox(height: 18),
            SizedBox(
              height: 28,
              width: 28,
              child: CircularProgressIndicator(
                strokeWidth: 3,
                color: colorScheme.primary,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
