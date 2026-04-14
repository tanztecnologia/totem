class AuthSession {
  const AuthSession({
    required this.tenantId,
    required this.userId,
    required this.email,
    required this.role,
    required this.token,
    required this.tenantName,
    required this.password,
  });

  final String tenantId;
  final String userId;
  final String email;
  final String role;
  final String token;
  final String tenantName;
  final String password;

  bool get isWaiter => role == 'Waiter';
  bool get isTotem => role == 'Totem';
  bool get isDashboard => role == 'Admin';
  bool get isKitchen => role == 'Staff';
  bool get isPdv => role == 'Pdv';
}
