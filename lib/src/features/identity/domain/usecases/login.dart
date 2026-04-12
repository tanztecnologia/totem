import '../entities/auth_session.dart';
import '../repositories/auth_repository.dart';

class Login {
  const Login(this._repo);

  final AuthRepository _repo;

  Future<AuthSession> call({
    required String tenantName,
    required String email,
    required String password,
  }) {
    return _repo.login(tenantName: tenantName, email: email, password: password);
  }
}

