import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uuid/uuid.dart';
import '../../../core/errors/result.dart';
import '../../../core/security/biometric_gate.dart';
import '../domain/payment.dart';

sealed class PaymentEvent extends Equatable {
  const PaymentEvent();
  @override
  List<Object?> get props => [];
}
final class PaymentSubmitted extends PaymentEvent {
  const PaymentSubmitted(this.request);
  final PaymentRequest request;
  @override
  List<Object?> get props => [request];
}

sealed class PaymentState extends Equatable {
  const PaymentState();
  @override
  List<Object?> get props => [];
}
final class PaymentIdle extends PaymentState { const PaymentIdle(); }
final class PaymentInProgress extends PaymentState { const PaymentInProgress(); }
final class PaymentSucceeded extends PaymentState {
  const PaymentSucceeded(this.payment);
  final Payment payment;
  @override
  List<Object?> get props => [payment];
}
final class PaymentFailed extends PaymentState {
  const PaymentFailed(this.message);
  final String message;
  @override
  List<Object?> get props => [message];
}

class PaymentBloc extends Bloc<PaymentEvent, PaymentState> {
  PaymentBloc(this._create, this._gate, {String Function()? keyFactory})
      : _newKey = keyFactory ?? (() => const Uuid().v4()),
        super(const PaymentIdle()) {
    on<PaymentSubmitted>(_onSubmitted);
  }
  final CreatePayment _create;
  final UserPresenceGate _gate;
  final String Function() _newKey;
  PaymentRequest? _pendingRequest;
  String? _pendingKey;

  Future<void> _onSubmitted(PaymentSubmitted e, Emitter<PaymentState> emit) async {
    if (state is PaymentInProgress) return; // ignore double taps
    emit(const PaymentInProgress());
    if (!await _gate.confirm('Confirm payment of ${e.request.amount.toStringAsFixed(2)} ${e.request.currency}')) {
      emit(const PaymentFailed('Payment cancelled: we could not confirm it was you.'));
      return;
    }
    // Retrying the SAME request re-uses the key, so a flaky network can never double-charge.
    if (_pendingRequest != e.request || _pendingKey == null) {
      _pendingRequest = e.request;
      _pendingKey = _newKey();
    }
    final result = await _create(e.request, idempotencyKey: _pendingKey!);
    switch (result) {
      case Ok(:final value):
        _pendingRequest = null;
        _pendingKey = null;
        emit(PaymentSucceeded(value));
      case Err(:final message):
        emit(PaymentFailed(message));
    }
  }
}
