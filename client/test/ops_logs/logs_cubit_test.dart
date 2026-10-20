import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:sentinelpay/core/errors/result.dart';
import 'package:sentinelpay/features/ops_logs/domain/log_entry.dart';
import 'package:sentinelpay/features/ops_logs/presentation/logs_cubit.dart';

class _Repo extends Mock implements LogRepository {}

void main() {
  test('tracing a correlation id queries only that request', () async {
    final repo = _Repo();
    when(() => repo.summary()).thenAnswer((_) async => const Ok(LogSummary(10, 1, [])));
    when(() => repo.query(level: any(named: 'level'), correlationId: any(named: 'correlationId'), text: any(named: 'text')))
        .thenAnswer((_) async => Ok([LogEntry(time: DateTime(2026), level: 'Error', message: 'Gateway timeout', correlationId: 'c1')]));
    final cubit = LogsCubit(repo);
    await cubit.trace('c1');
    verify(() => repo.query(level: null, correlationId: 'c1', text: '')).called(1);
    expect(cubit.state.entries.single.isFlaw, isTrue);
    await cubit.close();
  });
}
