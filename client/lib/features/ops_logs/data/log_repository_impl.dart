import 'package:dio/dio.dart';
import '../../../core/errors/result.dart';
import '../domain/log_entry.dart';

class LogRepositoryImpl implements LogRepository {
  LogRepositoryImpl(this._dio);
  final Dio _dio;

  @override
  Future<Result<List<LogEntry>>> query({String? level, String? correlationId, String? text}) async {
    try {
      final r = await _dio.get<List<dynamic>>('/api/ops/logs', queryParameters: {
        if (level != null) 'level': level, if (correlationId != null) 'correlationId': correlationId,
        if (text != null && text.isNotEmpty) 'q': text, 'take': 200 });
      return Ok(r.data!.cast<Map<String, dynamic>>().map((j) => LogEntry(
          time: DateTime.parse(j['time'] as String), level: j['level'] as String, message: j['message'] as String,
          correlationId: j['correlationId'] as String?, exception: j['exception'] as String?)).toList());
    } on DioException catch (e) {
      return Err(e.response?.statusCode == 403 ? 'You need the operations role to view logs.' : 'Could not load logs.');
    }
  }

  @override
  Future<Result<LogSummary>> summary() async {
    try {
      final r = await _dio.get<Map<String, dynamic>>('/api/ops/logs/summary');
      final byLevel = (r.data!['byLevel'] as Map<String, dynamic>);
      final errors = ((byLevel['Error'] ?? 0) as int) + ((byLevel['Fatal'] ?? 0) as int);
      final groups = (r.data!['topErrors'] as List<dynamic>).cast<Map<String, dynamic>>()
          .map((g) => ErrorGroup(g['signature'] as String, g['count'] as int, g['sampleCorrelationId'] as String?)).toList();
      return Ok(LogSummary(r.data!['total'] as int, errors, groups));
    } on DioException {
      return const Err('Could not load the log summary.');
    }
  }
}
