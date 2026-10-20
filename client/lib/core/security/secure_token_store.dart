import 'package:flutter_secure_storage/flutter_secure_storage.dart';

abstract interface class TokenStore {
  Future<String?> read();
  Future<void> write(String token);
  Future<void> clear();
}

/// Keystore (Android) / Keychain (iOS). Tokens never touch SharedPreferences or logs.
class SecureTokenStore implements TokenStore {
  SecureTokenStore([FlutterSecureStorage? s])
      : _s = s ??
            const FlutterSecureStorage(
              aOptions: AndroidOptions(encryptedSharedPreferences: true),
              iOptions: IOSOptions(accessibility: KeychainAccessibility.first_unlock_this_device),
            );
  final FlutterSecureStorage _s;
  static const _k = 'access_token';
  @override
  Future<String?> read() => _s.read(key: _k);
  @override
  Future<void> write(String token) => _s.write(key: _k, value: token);
  @override
  Future<void> clear() => _s.delete(key: _k);
}
