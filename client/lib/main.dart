import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'core/config/app_config.dart';
import 'core/security/biometric_gate.dart';
import 'core/security/secure_token_store.dart';
import 'features/ops_logs/domain/log_entry.dart';
import 'features/ops_logs/presentation/logs_cubit.dart';
import 'features/ops_logs/presentation/logs_page.dart';
import 'features/payments/domain/payment.dart';
import 'features/payments/presentation/pay_page.dart';
import 'features/payments/presentation/payment_bloc.dart';
import 'injection.dart';

// Local-development settings, passed on the command line (see docs/RUN_IN_VSCODE.md). Empty means production behaviour.
const _apiBase = String.fromEnvironment('API_BASE');
const _devToken = String.fromEnvironment('DEV_TOKEN');
const _accountId = String.fromEnvironment('ACCOUNT_ID', defaultValue: 'REPLACE_WITH_ACCOUNT_ID');
const _skipBiometric = bool.fromEnvironment('SKIP_BIOMETRIC');

class _AlwaysYes implements UserPresenceGate {
  @override
  Future<bool> confirm(String reason) async => true;
}

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  if (!kDebugMode && (_apiBase.isNotEmpty || _devToken.isNotEmpty || _skipBiometric)) {
    throw StateError('Development settings are not allowed in a release build.');
  }
  // With no pins, an https server is rejected (fails closed); plain http is for the local emulator only.
  final config = _apiBase.isEmpty ? AppConfig.prod : AppConfig(baseUrl: _apiBase, host: Uri.parse(_apiBase).host, pinnedSha256: const {});
  configureDependencies(config);
  if (_skipBiometric) {
    sl.unregister<UserPresenceGate>();
    sl.registerSingleton<UserPresenceGate>(_AlwaysYes());
  }
  if (_devToken.isNotEmpty) await sl<TokenStore>().write(_devToken);
  runApp(const SentinelPayApp());
}

class SentinelPayApp extends StatelessWidget {
  const SentinelPayApp({super.key});
  @override
  Widget build(BuildContext context) => MaterialApp(
        title: 'SentinelPay',
        theme: ThemeData(colorSchemeSeed: const Color(0xFF171A44), useMaterial3: true),
        darkTheme: ThemeData(colorSchemeSeed: const Color(0xFF171A44), brightness: Brightness.dark, useMaterial3: true),
        home: const _Home(),
      );
}

class _Home extends StatelessWidget {
  const _Home();
  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('SentinelPay')),
        body: ListView(padding: const EdgeInsets.all(16), children: [
          ListTile(title: const Text('Make a payment'), trailing: const Icon(Icons.chevron_right), onTap: () => Navigator.push(context, MaterialPageRoute<void>(
              builder: (_) => BlocProvider(create: (_) => PaymentBloc(sl<CreatePayment>(), sl<UserPresenceGate>()),
                  child: const PayPage(accountId: _accountId))))),
          ListTile(title: const Text('Run logs (operations)'), trailing: const Icon(Icons.chevron_right), onTap: () => Navigator.push(context, MaterialPageRoute<void>(
              builder: (_) => BlocProvider(create: (_) => LogsCubit(sl<LogRepository>())..load(), child: const LogsPage())))),
        ]),
      );
}
