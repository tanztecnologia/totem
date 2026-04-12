import '../entities/auth_session.dart';

abstract interface class AuthRepository {
  Future<AuthSession> login({
    required String tenantName,
    required String email,
    required String password,
  });
}

