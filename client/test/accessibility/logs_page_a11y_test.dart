import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:sentinelpay/core/errors/result.dart';
import 'package:sentinelpay/features/ops_logs/domain/log_entry.dart';
import 'package:sentinelpay/features/ops_logs/presentation/logs_cubit.dart';
import 'package:sentinelpay/features/ops_logs/presentation/logs_page.dart';

class _Repo extends Mock implements LogRepository {}

void main() {
  late _Repo repo;
  setUp(() {
    repo = _Repo();
    when(() => repo.summary()).thenAnswer((_) async => const Ok(LogSummary(120, 2, [ErrorGroup('TimeoutException: gateway', 2, 'c1')])));
    when(() => repo.query(level: any(named: 'level'), correlationId: any(named: 'correlationId'), text: any(named: 'text'))).thenAnswer((_) async => Ok([
          LogEntry(time: DateTime(2026, 10, 18, 9, 30), level: 'Information', message: 'Payment accepted', correlationId: 'c0'),
          LogEntry(time: DateTime(2026, 10, 18, 9, 31), level: 'Warning', message: 'Sync risk timed out', correlationId: 'c1'),
          LogEntry(time: DateTime(2026, 10, 18, 9, 32), level: 'Error', message: 'Gateway timeout', correlationId: 'c1', exception: 'TimeoutException: gateway'),
        ]));
  });

  Widget app({double textScale = 1.0}) => MaterialApp(
        builder: (c, child) => MediaQuery(data: MediaQuery.of(c).copyWith(textScaler: TextScaler.linear(textScale)), child: child!),
        home: BlocProvider(create: (_) => LogsCubit(repo)..load(), child: const LogsPage()),
      );

  testWidgets('status is conveyed by icon label and text, not colour alone', (t) async {
    final handle = t.ensureSemantics();
    await t.pumpWidget(app()); await t.pumpAndSettle();
    expect(find.bySemanticsLabel('Error'), findsWidgets);
    expect(find.bySemanticsLabel('Warning'), findsWidgets);
    expect(find.textContaining('Error'), findsWidgets);   // the level is also written as text
    handle.dispose();
  });

  testWidgets('tap targets and labels meet platform guidelines', (t) async {
    final handle = t.ensureSemantics();
    await t.pumpWidget(app()); await t.pumpAndSettle();
    await expectLater(t, meetsGuideline(androidTapTargetGuideline));
    await expectLater(t, meetsGuideline(labeledTapTargetGuideline));
    handle.dispose();
  });

  testWidgets('tapping an entry traces that request', (t) async {
    await t.pumpWidget(app()); await t.pumpAndSettle();
    await t.tap(find.text('Gateway timeout')); await t.pumpAndSettle();
    expect(find.textContaining('Tracing request c1'), findsOneWidget);
    verify(() => repo.query(level: null, correlationId: 'c1', text: '')).called(1);
  });

  testWidgets('layout survives 200% text size', (t) async {
    await t.pumpWidget(app(textScale: 2.0)); await t.pumpAndSettle();
    expect(t.takeException(), isNull);   // if this fails it is a real accessibility defect to fix, not a test to loosen
  });
}
