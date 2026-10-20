import 'dart:io';
import 'package:crypto/crypto.dart';
import 'package:dio/dio.dart';
import 'package:dio/io.dart';
import 'package:flutter/foundation.dart';
import 'package:uuid/uuid.dart';
import '../config/app_config.dart';
import '../security/secure_token_store.dart';

class ApiClient {
  ApiClient(AppConfig config, TokenStore tokens)
      : dio = Dio(BaseOptions(
          baseUrl: config.baseUrl,
          connectTimeout: const Duration(seconds: 10),
          receiveTimeout: const Duration(seconds: 20),
          headers: {'Accept': 'application/json'},
        )) {
    dio.httpClientAdapter = IOHttpClientAdapter(
      createHttpClient: () => HttpClient()..badCertificateCallback = (_, __, ___) => false, // never trust bad certs
      validateCertificate: (cert, host, port) {
        if (cert == null || host != config.host) return false;
        final fingerprint = sha256.convert(cert.der).toString();
        final pinned = config.pinnedSha256.contains(fingerprint);
        if (!pinned && kDebugMode) debugPrint('Certificate pin mismatch for $host');
        return pinned;
      },
    );
    dio.interceptors.add(InterceptorsWrapper(onRequest: (o, h) async {
      final t = await tokens.read();
      if (t != null) o.headers['Authorization'] = 'Bearer $t';
      o.headers['X-Correlation-Id'] = const Uuid().v4().replaceAll('-', ''); // same id appears in server logs and agent audit rows
      h.next(o);
    }));
  }
  final Dio dio;
}
