import 'package:local_auth/local_auth.dart';

abstract interface class UserPresenceGate {
  /// True when the person proved presence (biometric or device passcode).
  Future<bool> confirm(String reason);
}

class BiometricGate implements UserPresenceGate {
  BiometricGate([LocalAuthentication? a]) : _a = a ?? LocalAuthentication();
  final LocalAuthentication _a;
  @override
  Future<bool> confirm(String reason) async {
    try {
      if (!await _a.isDeviceSupported()) return false; // fail closed: no secure lock screen, no payment
      return await _a.authenticate(
        localizedReason: reason,
        options: const AuthenticationOptions(stickyAuth: true), // passcode fallback allowed for accessibility
      );
    } on Exception {
      return false;
    }
  }
}
