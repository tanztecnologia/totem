import 'package:flutter_bloc/flutter_bloc.dart';

import '../../domain/entities/auth_session.dart';
import '../../domain/usecases/login.dart';
import 'auth_state.dart';

class AuthCubit extends Cubit<AuthState> {
  AuthCubit({
    required Login login,
  })  : _login = login,
        super(const AuthUnauthenticated());

  final Login _login;

  AuthSession? get session => switch (state) {
        AuthAuthenticated(:final session) => session,
        _ => null,
      };

  Future<void> login({
    required String tenantName,
    required String email,
    required String password,
  }) async {
    emit(const AuthAuthenticating());
    try {
      final session = await _login(
        tenantName: tenantName,
        email: email,
        password: password,
      );
      emit(AuthAuthenticated(session));
    } catch (e) {
      emit(AuthFailure(e.toString()));
      emit(const AuthUnauthenticated());
    }
  }

  void logout() {
    emit(const AuthUnauthenticated());
  }
}

