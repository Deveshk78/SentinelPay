import 'package:flutter_bloc/flutter_bloc.dart';
import '../../../core/errors/result.dart';
import '../domain/assistant.dart';

sealed class AssistantState { const AssistantState(); }
final class AssistantIdle extends AssistantState { const AssistantIdle(); }
final class AssistantLoading extends AssistantState { const AssistantLoading(); }
final class AssistantDone extends AssistantState { const AssistantDone(this.answer); final AssistantAnswer answer; }
final class AssistantError extends AssistantState { const AssistantError(this.message); final String message; }

class AssistantCubit extends Cubit<AssistantState> {
  AssistantCubit(this._repo, this.accountId) : super(const AssistantIdle());
  final AssistantRepository _repo; final String accountId;
  Future<void> ask(String q) async {
    if (q.trim().isEmpty) return;
    emit(const AssistantLoading());
    emit(switch (await _repo.ask(accountId, q.trim())) { Ok(:final value) => AssistantDone(value), Err(:final message) => AssistantError(message) });
  }
}
