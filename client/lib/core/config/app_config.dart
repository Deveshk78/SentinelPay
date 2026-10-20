class AppConfig {
  const AppConfig({required this.baseUrl, required this.host, required this.pinnedSha256});
  final String baseUrl;
  final String host;

  /// SHA-256 of the server certificate (DER, lowercase hex). Always ship the current pin AND a backup pin,
  /// so a certificate rotation does not lock users out. Update via app release, not remote config.
  final Set<String> pinnedSha256;

  static const prod = AppConfig(
    baseUrl: 'https://api.sentinelpay.example',
    host: 'api.sentinelpay.example',
    pinnedSha256: {'REPLACE_WITH_CURRENT_PIN', 'REPLACE_WITH_BACKUP_PIN'},
  );
}
