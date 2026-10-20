import 'package:dio/dio.dart';
import '../../../core/errors/result.dart';
import '../domain/payment.dart';

class PaymentRepositoryImpl implements PaymentRepository {
  PaymentRepositoryImpl(this._dio);
  final Dio _dio;

  @override
  Future<Result<Payment>> create(PaymentRequest r, {required String idempotencyKey}) async {
    try {
      final res = await _dio.post<Map<String, dynamic>>('/api/payments',
          data: {'accountId': r.accountId, 'amount': r.amount, 'currency': r.currency, 'merchantId': r.merchantId},
          options: Options(headers: {'Idempotency-Key': idempotencyKey}));
      return Ok(_map(res.data!));
    } on DioException catch (e) {
      final body = e.response?.data;
      // 422 carries a PaymentResult (declined/rejected) with a human-readable message.
      if (body is Map<String, dynamic> && body['message'] is String) return Err(body['message'] as String, code: e.response?.statusCode);
      return Err(_friendly(e.response?.statusCode), code: e.response?.statusCode);
    }
  }

  Payment _map(Map<String, dynamic> j) => Payment(
      id: (j['transactionId'] ?? '') as String, status: j['status'] as String,
      riskScore: j['riskScore'] as int?, message: j['message'] as String?);

  String _friendly(int? code) => switch (code) {
        401 => 'Your session has expired. Please sign in again.',
        429 => 'Too many attempts. Wait a minute and try again.',
        null => 'No connection. Your payment was not sent.',
        _ => 'Something went wrong. Your payment was not completed.',
      };
}
