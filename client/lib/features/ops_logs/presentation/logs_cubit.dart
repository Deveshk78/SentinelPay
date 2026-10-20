import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../core/errors/result.dart';
import '../domain/log_entry.dart';

class LogsState extends Equatable {
  const LogsState({this.loading = false, this.entries = const [], this.summary, this.error, this.level, this.traceId, this.text = ''});
  final bool loading; final List<LogEntry> entries; final LogSummary? summary; final String? error;
  final String? level; final String? traceId; final String text;
  LogsState copyWith({bool? loading, List<LogEntry>? entries, LogSummary? summary, String? error, String? level, String? traceId, String? text,
          bool clearError = false, bool clearTrace = false, bool clearLevel = false}) =>
      LogsState(loading: loading ?? this.loading, entries: entries ?? this.entries, summary: summary ?? this.summary,
          error: clearError ? null : error ?? this.error, level: clearLevel ? null : level ?? this.level,
          traceId: clearTrace ? null : traceId ?? this.traceId, text: text ?? this.text);
  @override
  List<Object?> get props => [loading, entries, summary, error, level, traceId, text];
}

/// Tap an entry's correlation id to see the whole journey of one request: API, outbox, agents, gateway.
class LogsCubit extends Cubit<LogsState> {
  LogsCubit(this._repo) : super(const LogsState());
  final LogRepository _repo;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearError: true));
    final s = await _repo.summary();
    final e = await _repo.query(level: state.level, correlationId: state.traceId, text: state.text);
    emit(state.copyWith(loading: false,
        summary: s is Ok<LogSummary> ? s.value : state.summary,
        entries: e is Ok<List<LogEntry>> ? e.value : state.entries,
        error: e is Err<List<LogEntry>> ? e.message : null, clearError: e is Ok));
  }
  Future<void> setLevel(String? level) { emit(state.copyWith(level: level, clearLevel: level == null)); return load(); }
  Future<void> setText(String text) { emit(state.copyWith(text: text)); return load(); }
  Future<void> trace(String correlationId) { emit(state.copyWith(traceId: correlationId, clearLevel: true)); return load(); }
  Future<void> clearTrace() { emit(state.copyWith(clearTrace: true)); return load(); }
}
