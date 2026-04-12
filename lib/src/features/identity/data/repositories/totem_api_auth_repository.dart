import 'package:totem/src/http/totem_http.dart';

import '../../domain/entities/auth_session.dart';
import '../../domain/repositories/auth_repository.dart';

class TotemApiAuthRepository implements AuthRepository {
  TotemApiAuthRepository({
    required Uri baseUrl,
    TotemHttpClient? httpClient,
  }) : _http = httpClient ?? TotemHttpClient(baseUrl: baseUrl);

  final TotemHttpClient _http;

  @override
  Future<AuthSession> login({
    required String tenantName,
    required String email,
    required String password,
  }) async {
    final trimmedTenant = tenantName.trim();
    final trimmedEmail = email.trim();
    final trimmedPassword = password;

    if (trimmedTenant.isEmpty) {
      throw Exception('Informe o tenant');
    }
    if (trimmedEmail.isEmpty) {
      throw Exception('Informe o email');
    }
    if (trimmedPassword.trim().isEmpty) {
      throw Exception('Informe a senha');
    }

    final resp = await _http.postJson<Map<String, dynamic>>(
      '/api/auth/login',
      body: <String, Object?>{
        'tenantName': trimmedTenant,
        'email': trimmedEmail,
        'password': trimmedPassword,
      },
    );

    final token = ((resp['token'] ?? resp['Token']) as String?)?.trim() ?? '';
    final role = ((resp['role'] ?? resp['Role']) as String?)?.trim() ?? '';
    final tenantId = ((resp['tenantId'] ?? resp['TenantId']) as String?)?.trim() ?? '';
    final userId = ((resp['userId'] ?? resp['UserId']) as String?)?.trim() ?? '';
    final responseEmail = ((resp['email'] ?? resp['Email']) as String?)?.trim() ?? trimmedEmail;

    if (token.isEmpty) throw Exception('Resposta inválida do login (token ausente).');
    if (role.isEmpty) throw Exception('Resposta inválida do login (role ausente).');
    if (tenantId.isEmpty) throw Exception('Resposta inválida do login (tenantId ausente).');
    if (userId.isEmpty) throw Exception('Resposta inválida do login (userId ausente).');

    return AuthSession(
      tenantId: tenantId,
      userId: userId,
      email: responseEmail,
      role: role,
      token: token,
      tenantName: trimmedTenant,
      password: trimmedPassword,
    );
  }
}

