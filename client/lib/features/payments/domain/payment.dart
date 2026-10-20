import 'package:equatable/equatable.dart';
import '../../../core/errors/result.dart';

class Payment extends Equatable {
  const Payment({required this.id, required this.status, this.riskScore, this.message});
  final String id;
  final String status; // Approved | PendingRisk | RiskReview | Declined | Settled ...
  final int? riskScore;
  final String? message;
  @override
  List<Object?> get props => [id, status, riskScore, message];
}

class PaymentRequest extends Equatable {
  const PaymentRequest({required this.accountId, required this.amount, required this.currency, required this.merchantId});
  final String accountId;
  final double amount;
  final String currency;
  final String merchantId;
  @override
  List<Object?> get props => [accountId, amount, currency, merchantId];
}

abstract interface class PaymentRepository {
  Future<Result<Payment>> create(PaymentRequest request, {required String idempotencyKey});
}

class CreatePayment {
  const CreatePayment(this._repo);
  final PaymentRepository _repo;
  Future<Result<Payment>> call(PaymentRequest r, {required String idempotencyKey}) {
    if (r.amount <= 0) return Future.value(const Err('Enter an amount greater than zero.'));
    if (r.merchantId.trim().isEmpty) return Future.value(const Err('Enter who you are paying.'));
    return _repo.create(r, idempotencyKey: idempotencyKey);
  }
}
