import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter/services.dart';
import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:totem_ds/totem_ds.dart';

import 'src/features/checkout/data/services/fake_checkout_service.dart';
import 'src/features/checkout/data/services/totem_api_checkout_service.dart';
import 'src/features/checkout/domain/services/checkout_service.dart';
import 'src/features/identity/data/repositories/totem_api_auth_repository.dart';
import 'src/features/identity/domain/repositories/auth_repository.dart';
import 'src/features/identity/domain/usecases/login.dart';
import 'src/features/identity/presentation/bloc/auth_cubit.dart';
import 'src/features/identity/presentation/bloc/auth_state.dart';
import 'src/features/identity/presentation/pages/login_page.dart';
import 'src/features/kiosk/data/repositories/api_catalog_repository.dart';
import 'src/features/kiosk/data/repositories/in_memory_catalog_repository.dart';
import 'src/features/kiosk/domain/repositories/catalog_repository.dart';
import 'src/features/kiosk/presentation/bloc/kiosk_bloc.dart';
import 'src/features/kiosk/presentation/pages/kiosk_page.dart';

import 'src/features/kitchen/data/repositories/api_kitchen_repository.dart';
import 'src/features/kitchen/data/repositories/in_memory_kitchen_repository.dart';
import 'src/features/kitchen/domain/repositories/kitchen_repository.dart';
import 'src/features/kitchen/presentation/bloc/kitchen_cubit.dart';
import 'src/features/kitchen/presentation/pages/kitchen_page.dart';
import 'src/features/waiter/presentation/pages/waiter_page.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await SystemChrome.setEnabledSystemUIMode(SystemUiMode.immersiveSticky);
  await dotenv.load(fileName: '.env');
  runApp(const TotemApp());
}

class TotemApp extends StatelessWidget {
  const TotemApp({super.key});

  Uri? _tryParseAndSanitizeBaseUrl(String raw) {
    final trimmed = raw.trim();
    if (trimmed.isEmpty) return null;

    final uri = Uri.tryParse(trimmed);
    if (uri == null || !uri.hasScheme || uri.host.isEmpty) return null;

    if (uri.host == '0.0.0.0') {
      return uri.replace(host: 'localhost');
    }
    return uri;
  }

  @override
  Widget build(BuildContext context) {
    final env = dotenv.isInitialized ? dotenv.env : const <String, String>{};
    final apiBaseUrl =
        env['TOTEM_API_BASE_URL'] ?? const String.fromEnvironment('TOTEM_API_BASE_URL');
    final tenantName =
        env['TOTEM_TENANT_NAME'] ?? const String.fromEnvironment('TOTEM_TENANT_NAME');
    final email = env['TOTEM_EMAIL'] ?? const String.fromEnvironment('TOTEM_EMAIL');
    final password = env['TOTEM_PASSWORD'] ?? const String.fromEnvironment('TOTEM_PASSWORD');
    final kitchenEmail =
        env['TOTEM_KITCHEN_EMAIL'] ?? const String.fromEnvironment('TOTEM_KITCHEN_EMAIL');
    final kitchenPassword = env['TOTEM_KITCHEN_PASSWORD'] ??
        const String.fromEnvironment('TOTEM_KITCHEN_PASSWORD');
    final appMode =
        env['APP_MODE'] ?? const String.fromEnvironment('APP_MODE', defaultValue: 'kiosk');

    final effectiveKitchenEmail = kitchenEmail.trim().isEmpty ? email : kitchenEmail;
    final effectiveKitchenPassword = kitchenPassword.trim().isEmpty ? password : kitchenPassword;
    final sanitizedBaseUrl = _tryParseAndSanitizeBaseUrl(apiBaseUrl);

    if (sanitizedBaseUrl != null) {
      return RepositoryProvider<AuthRepository>(
        create: (_) => TotemApiAuthRepository(baseUrl: sanitizedBaseUrl),
        child: BlocProvider(
          create: (context) => AuthCubit(login: Login(context.read<AuthRepository>())),
          child: MaterialApp(
            title: 'TZTotem',
            theme: TotemTheme.light(),
            home: _SplashGate(
              child: _AuthedApp(
                baseUrl: sanitizedBaseUrl,
                initialTenantName: tenantName,
                initialEmail: email,
              ),
            ),
          ),
        ),
      );
    }

    final Widget homeFeature;

    if (appMode == 'kitchen') {
      homeFeature = BlocProvider(
        create: (context) => KitchenCubit(
          context.read<KitchenRepository>(),
        )..loadOrders(),
        child: const KitchenPage(),
      );
    } else if (appMode == 'waiter') {
      homeFeature = BlocProvider(
        create: (context) => KioskBloc(
          catalogRepository: context.read<CatalogRepository>(),
        )..add(const KioskLoadRequested()),
        child: const WaiterPage(),
      );
    } else {
      homeFeature = BlocProvider(
        create: (context) => KioskBloc(
          catalogRepository: context.read<CatalogRepository>(),
        )..add(const KioskLoadRequested()),
        child: const KioskPage(),
      );
    }

    return MultiRepositoryProvider(
      providers: [
        RepositoryProvider<CatalogRepository>(
          create: (_) {
            if (sanitizedBaseUrl == null) return const InMemoryCatalogRepository();
            return ApiCatalogRepository(
              baseUrl: sanitizedBaseUrl,
              tenantName: tenantName,
              email: email,
              password: password,
            );
          },
        ),
        RepositoryProvider<CheckoutService>(
          create: (_) {
            if (sanitizedBaseUrl == null) return FakeCheckoutService();
            return TotemApiCheckoutService(
              baseUrl: sanitizedBaseUrl,
              tenantName: tenantName,
              email: email,
              password: password,
            );
          },
        ),
        RepositoryProvider<KitchenRepository>(
          create: (_) {
            if (sanitizedBaseUrl == null) return InMemoryKitchenRepository();
            return ApiKitchenRepository(
              baseUrl: sanitizedBaseUrl,
              tenantName: tenantName,
              email: effectiveKitchenEmail,
              password: effectiveKitchenPassword,
            );
          },
        ),
      ],
      child: MaterialApp(
        title: 'TZTotem',
        theme: TotemTheme.light(),
        home: _SplashGate(child: homeFeature),
      ),
    );
  }
}

class _AuthedApp extends StatelessWidget {
  const _AuthedApp({
    required this.baseUrl,
    required this.initialTenantName,
    required this.initialEmail,
  });

  final Uri baseUrl;
  final String initialTenantName;
  final String initialEmail;

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<AuthCubit, AuthState>(
      builder: (context, state) {
        if (state is AuthAuthenticated) {
          final session = state.session;

          if (session.isTotem || session.isWaiter) {
            return MultiRepositoryProvider(
              providers: [
                RepositoryProvider<CatalogRepository>(
                  create: (_) => ApiCatalogRepository(
                    baseUrl: baseUrl,
                    tenantName: session.tenantName,
                    email: session.email,
                    password: session.password,
                    initialToken: session.token,
                  ),
                ),
                RepositoryProvider<CheckoutService>(
                  create: (_) => TotemApiCheckoutService(
                    baseUrl: baseUrl,
                    tenantName: session.tenantName,
                    email: session.email,
                    password: session.password,
                    initialToken: session.token,
                  ),
                ),
              ],
              child: BlocProvider(
                create: (context) => KioskBloc(
                  catalogRepository: context.read<CatalogRepository>(),
                )..add(const KioskLoadRequested()),
                child: session.isWaiter ? const WaiterPage() : const KioskPage(),
              ),
            );
          }

          if (session.isKitchen) {
            return RepositoryProvider<KitchenRepository>(
              create: (_) => ApiKitchenRepository(
                baseUrl: baseUrl,
                tenantName: session.tenantName,
                email: session.email,
                password: session.password,
                initialToken: session.token,
              ),
              child: BlocProvider(
                create: (context) => KitchenCubit(
                  context.read<KitchenRepository>(),
                )..loadOrders(),
                child: const KitchenPage(),
              ),
            );
          }

          return Scaffold(
            body: Center(
              child: Text('Perfil não suportado: ${session.role}'),
            ),
          );
        }

        return LoginPage(
          initialTenantName: initialTenantName,
          initialEmail: initialEmail,
        );
      },
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
