import 'package:equatable/equatable.dart';
import '../../../core/errors/result.dart';

class LogEntry extends Equatable {
  const LogEntry({required this.time, required this.level, required this.message, this.correlationId, this.exception});
  final DateTime time; final String level; final String message; final String? correlationId; final String? exception;
  bool get isFlaw => level == 'Error' || level == 'Fatal';
  @override
  List<Object?> get props => [time, level, message, correlationId, exception];
}
class ErrorGroup extends Equatable {
  const ErrorGroup(this.signature, this.count, this.sampleCorrelationId);
  final String signature; final int count; final String? sampleCorrelationId;
  @override
  List<Object?> get props => [signature, count, sampleCorrelationId];
}
class LogSummary extends Equatable {
  const LogSummary(this.total, this.errors, this.topErrors);
  final int total; final int errors; final List<ErrorGroup> topErrors;
  @override
  List<Object?> get props => [total, errors, topErrors];
}
abstract interface class LogRepository {
  Future<Result<List<LogEntry>>> query({String? level, String? correlationId, String? text});
  Future<Result<LogSummary>> summary();
}
