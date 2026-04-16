import 'package:flutter_test/flutter_test.dart';
import 'package:totem/src/features/identity/domain/entities/auth_session.dart';
import 'package:totem/src/features/identity/domain/repositories/auth_repository.dart';
import 'package:totem/src/features/identity/domain/usecases/login.dart';
import 'package:totem/src/features/identity/presentation/bloc/auth_cubit.dart';
import 'package:totem/src/features/identity/presentation/bloc/auth_state.dart';

void main() {
  test('AuthCubit inicia deslogado', () {
    final cubit = AuthCubit(login: Login(_FakeAuthRepository()));
    addTearDown(cubit.close);

    expect(cubit.state, const AuthUnauthenticated());
  });

  test('AuthCubit autentica e emite sessão', () async {
    final repo = _FakeAuthRepository(
      session: const AuthSession(
        tenantId: 't1',
        userId: 'u1',
        email: 'waiter@a.com',
        role: 'Waiter',
        token: 'token-123',
        tenantName: 'Empresa X',
        password: '123456',
        permissions: <String>['checkout:write'],
      ),
    );
    final cubit = AuthCubit(login: Login(repo));
    addTearDown(cubit.close);

    expectLater(
      cubit.stream,
      emitsInOrder(
        [
          isA<AuthAuthenticating>(),
          isA<AuthAuthenticated>()
              .having((s) => s.session.role, 'role', 'Waiter')
              .having((s) => s.session.token, 'token', 'token-123'),
        ],
      ),
    );

    await cubit.login(tenantName: 'Empresa X', email: 'waiter@a.com', password: '123456');
  });

  test('AuthCubit falha e volta para deslogado', () async {
    final cubit = AuthCubit(login: Login(_FakeAuthRepository(throwOnLogin: true)));
    addTearDown(cubit.close);

    expectLater(
      cubit.stream,
      emitsInOrder(
        [
          isA<AuthAuthenticating>(),
          isA<AuthFailure>(),
          isA<AuthUnauthenticated>(),
        ],
      ),
    );

    await cubit.login(tenantName: 'Empresa X', email: 'x@a.com', password: 'wrong');
  });
}

class _FakeAuthRepository implements AuthRepository {
  _FakeAuthRepository({
    this.throwOnLogin = false,
    AuthSession? session,
  }) : _session = session ??
            const AuthSession(
              tenantId: 't',
              userId: 'u',
              email: 'admin@a.com',
              role: 'Totem',
              token: 'token',
              tenantName: 'Empresa X',
              password: '123456',
              permissions: <String>['checkout:write'],
            );

  final bool throwOnLogin;
  final AuthSession _session;

  @override
  Future<AuthSession> login({
    required String tenantName,
    required String email,
    required String password,
  }) async {
    if (throwOnLogin) throw Exception('Credenciais inválidas');
    return _session;
  }
}
