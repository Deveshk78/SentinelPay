import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../domain/payment.dart';
import 'payment_bloc.dart';

class PayPage extends StatefulWidget {
  const PayPage({super.key, required this.accountId});
  final String accountId;
  @override
  State<PayPage> createState() => _PayPageState();
}

class _PayPageState extends State<PayPage> {
  final _amount = TextEditingController();
  final _merchant = TextEditingController();
  @override
  void dispose() { _amount.dispose(); _merchant.dispose(); super.dispose(); }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Make a payment')),
        body: BlocConsumer<PaymentBloc, PaymentState>(
          listener: (c, s) {
            if (s is PaymentSucceeded) {
              ScaffoldMessenger.of(c).showSnackBar(SnackBar(content: Text(s.payment.message ?? 'Payment accepted.')));
            }
          },
          builder: (c, s) => ListView(padding: const EdgeInsets.all(16), children: [
            TextField(
              controller: _amount,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: const InputDecoration(labelText: 'Amount (INR)'),
            ),
            const SizedBox(height: 12),
            TextField(controller: _merchant, decoration: const InputDecoration(labelText: 'Pay to (merchant ID)')),
            const SizedBox(height: 20),
            ConstrainedBox( // 48dp minimum touch target
              constraints: const BoxConstraints(minHeight: 48),
              child: FilledButton(
                onPressed: s is PaymentInProgress ? null : () => _submit(c),
                child: Text(s is PaymentInProgress ? 'Paying…' : 'Pay'),
              ),
            ),
            // liveRegion makes screen readers announce the result without moving focus.
            if (s is PaymentFailed) Semantics(liveRegion: true, child: Padding(padding: const EdgeInsets.only(top: 16),
                child: Text(s.message, style: TextStyle(color: Theme.of(c).colorScheme.error)))),
            if (s is PaymentSucceeded) Semantics(liveRegion: true, child: Padding(padding: const EdgeInsets.only(top: 16),
                child: Text('${s.payment.message ?? s.payment.status}'))),
          ]),
        ),
      );

  void _submit(BuildContext c) {
    final amount = double.tryParse(_amount.text.trim()) ?? 0;
    c.read<PaymentBloc>().add(PaymentSubmitted(PaymentRequest(
        accountId: widget.accountId, amount: amount, currency: 'INR', merchantId: _merchant.text.trim())));
  }
}
