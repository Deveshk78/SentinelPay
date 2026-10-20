import 'package:get_it/get_it.dart';
import 'core/config/app_config.dart';
import 'core/network/api_client.dart';
import 'core/security/biometric_gate.dart';
import 'core/security/secure_token_store.dart';
import 'features/assistant/data/assistant_repository_impl.dart';
import 'features/assistant/domain/assistant.dart';
import 'features/ops_logs/data/log_repository_impl.dart';
import 'features/ops_logs/domain/log_entry.dart';
import 'features/payments/data/payment_repository_impl.dart';
import 'features/payments/domain/payment.dart';

final sl = GetIt.instance;

void configureDependencies(AppConfig config) {
  sl
    ..registerSingleton<AppConfig>(config)
    ..registerSingleton<TokenStore>(SecureTokenStore())
    ..registerSingleton<UserPresenceGate>(BiometricGate())
    ..registerSingleton<ApiClient>(ApiClient(config, sl<TokenStore>()))
    ..registerLazySingleton<PaymentRepository>(() => PaymentRepositoryImpl(sl<ApiClient>().dio))
    ..registerLazySingleton<AssistantRepository>(() => AssistantRepositoryImpl(sl<ApiClient>().dio))
    ..registerLazySingleton<LogRepository>(() => LogRepositoryImpl(sl<ApiClient>().dio))
    ..registerLazySingleton(() => CreatePayment(sl<PaymentRepository>()));
}
