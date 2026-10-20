import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:sentinelpay/core/errors/result.dart';
import 'package:sentinelpay/core/security/biometric_gate.dart';
import 'package:sentinelpay/features/payments/domain/payment.dart';
import 'package:sentinelpay/features/payments/presentation/payment_bloc.dart';

class _Repo extends Mock implements PaymentRepository {}
class _Gate extends Mock implements UserPresenceGate {}

void main() {
  const req = PaymentRequest(accountId: 'a1', amount: 250, currency: 'INR', merchantId: 'shop-1');
  const ok = Payment(id: 'p1', status: 'Approved', message: 'Payment accepted.');
  late _Repo repo; late _Gate gate; var n = 0;

  setUpAll(() => registerFallbackValue(req));   // mocktail needs this for any() on a custom type
  setUp(() { repo = _Repo(); gate = _Gate(); n = 0; when(() => gate.confirm(any())).thenAnswer((_) async => true); });
  PaymentBloc build() => PaymentBloc(CreatePayment(repo), gate, keyFactory: () => 'key-${++n}');

  blocTest<PaymentBloc, PaymentState>('succeeds after biometric confirmation',
    build: () { when(() => repo.create(req, idempotencyKey: any(named: 'idempotencyKey'))).thenAnswer((_) async => const Ok(ok)); return build(); },
    act: (b) => b.add(const PaymentSubmitted(req)),
    expect: () => [const PaymentInProgress(), const PaymentSucceeded(ok)]);

  blocTest<PaymentBloc, PaymentState>('does not call the server when biometrics fail',
    build: () { when(() => gate.confirm(any())).thenAnswer((_) async => false); return build(); },
    act: (b) => b.add(const PaymentSubmitted(req)),
    expect: () => [const PaymentInProgress(), isA<PaymentFailed>()],
    verify: (_) => verifyNever(() => repo.create(any(), idempotencyKey: any(named: 'idempotencyKey'))));

  test('a retry after a network failure re-uses the same idempotency key', () async {
    final keys = <String>[];
    when(() => repo.create(req, idempotencyKey: any(named: 'idempotencyKey'))).thenAnswer((i) async {
      keys.add(i.namedArguments[#idempotencyKey] as String);
      return keys.length == 1 ? const Err('No connection.') : const Ok(ok);
    });
    final b = build();
    b.add(const PaymentSubmitted(req)); await b.stream.firstWhere((s) => s is PaymentFailed);
    b.add(const PaymentSubmitted(req)); await b.stream.firstWhere((s) => s is PaymentSucceeded);
    expect(keys, ['key-1', 'key-1']);
    await b.close();
  });

  test('use case rejects non-positive amounts before any network call', () async {
    final r = await CreatePayment(repo)(const PaymentRequest(accountId: 'a', amount: 0, currency: 'INR', merchantId: 'm'), idempotencyKey: 'k');
    expect(r, isA<Err<Payment>>());
    verifyNever(() => repo.create(any(), idempotencyKey: any(named: 'idempotencyKey')));
  });
}
