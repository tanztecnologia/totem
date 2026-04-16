class AuthSession {
  const AuthSession({
    required this.tenantId,
    required this.userId,
    required this.email,
    required this.role,
    required this.token,
    required this.tenantName,
    required this.password,
    required this.permissions,
  });

  final String tenantId;
  final String userId;
  final String email;
  final String role;
  final String token;
  final String tenantName;
  final String password;
  final List<String> permissions;

  bool hasPermission(String p) => permissions.contains(p);

  bool get isWaiter => role == 'Waiter';
  bool get isTotem => hasPermission('checkout:write') && role == 'Totem';
  bool get isDashboard => hasPermission('dashboard:read');
  bool get isKitchen => hasPermission('kitchen:write');
  bool get isPdv => hasPermission('pos:write');
}
