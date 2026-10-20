import 'package:dio/dio.dart';
import '../../../core/errors/result.dart';
import '../domain/assistant.dart';

class AssistantRepositoryImpl implements AssistantRepository {
  AssistantRepositoryImpl(this._dio);
  final Dio _dio;
  @override
  Future<Result<AssistantAnswer>> ask(String accountId, String question) async {
    try {
      final r = await _dio.post<Map<String, dynamic>>('/api/assistant/query', data: {'accountId': accountId, 'question': question});
      final items = (r.data!['items'] as List<dynamic>).cast<Map<String, dynamic>>().map((j) => AssistantItem(
          j['id'] as String, (j['amount'] as num).toDouble(), j['currency'] as String, j['merchantId'] as String, j['status'] as String)).toList();
      return Ok(AssistantAnswer(r.data!['summary'] as String, items));
    } on DioException catch (e) {
      final b = e.response?.data;
      return Err(b is Map<String, dynamic> && b['error'] is String ? b['error'] as String : 'The assistant is unavailable right now.');
    }
  }
}
