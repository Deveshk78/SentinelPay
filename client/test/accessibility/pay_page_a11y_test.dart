import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:sentinelpay/core/security/biometric_gate.dart';
import 'package:sentinelpay/features/payments/domain/payment.dart';
import 'package:sentinelpay/features/payments/presentation/pay_page.dart';
import 'package:sentinelpay/features/payments/presentation/payment_bloc.dart';

class _Repo extends Mock implements PaymentRepository {}
class _Gate extends Mock implements UserPresenceGate {}

void main() {
  late PaymentBloc bloc;
  late _Gate gate;

  setUp(() {
    gate = _Gate();
    when(() => gate.confirm(any())).thenAnswer((_) async => false);
    bloc = PaymentBloc(CreatePayment(_Repo()), gate);
  });
  tearDown(() => bloc.close());

  Widget app({double textScale = 1.0}) => MaterialApp(
        builder: (c, child) => MediaQuery(data: MediaQuery.of(c).copyWith(textScaler: TextScaler.linear(textScale)), child: child!),
        home: BlocProvider<PaymentBloc>.value(value: bloc, child: const PayPage(accountId: 'a1')),
      );

  testWidgets('tap targets, labels and contrast meet platform guidelines', (t) async {
    final handle = t.ensureSemantics();
    await t.pumpWidget(app());
    await expectLater(t, meetsGuideline(androidTapTargetGuideline));
    await expectLater(t, meetsGuideline(iOSTapTargetGuideline));
    await expectLater(t, meetsGuideline(labeledTapTargetGuideline));
    await expectLater(t, meetsGuideline(textContrastGuideline));
    handle.dispose();
  });

  testWidgets('layout survives 200% text size without overflow', (t) async {
    await t.pumpWidget(app(textScale: 2.0));
    expect(t.takeException(), isNull);
    expect(find.text('Pay'), findsOneWidget);
  });

  testWidgets('every input is reachable by its label', (t) async {
    final handle = t.ensureSemantics();
    await t.pumpWidget(app());
    expect(find.bySemanticsLabel(RegExp('Amount')), findsOneWidget);
    expect(find.bySemanticsLabel(RegExp('Pay to')), findsOneWidget);
    handle.dispose();
  });

  testWidgets('a failure message is announced to screen readers as a live region', (t) async {
    final handle = t.ensureSemantics();
    await t.pumpWidget(app());
    await t.enterText(find.byType(TextField).first, '250');
    await t.enterText(find.byType(TextField).last, 'shop-1');
    await t.tap(find.text('Pay'));
    await t.pumpAndSettle();
    expect(t.getSemantics(find.textContaining('could not confirm')), containsSemantics(isLiveRegion: true));
    handle.dispose();
  });
}
