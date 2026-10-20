import '../../../core/errors/result.dart';

class AssistantItem {
  const AssistantItem(this.id, this.amount, this.currency, this.merchantId, this.status);
  final String id; final double amount; final String currency; final String merchantId; final String status;
}
class AssistantAnswer {
  const AssistantAnswer(this.summary, this.items);
  final String summary; final List<AssistantItem> items;
}
abstract interface class AssistantRepository {
  Future<Result<AssistantAnswer>> ask(String accountId, String question);
}
