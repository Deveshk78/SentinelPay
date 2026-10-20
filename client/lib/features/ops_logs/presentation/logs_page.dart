import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../domain/log_entry.dart';
import 'logs_cubit.dart';

class LogsPage extends StatelessWidget {
  const LogsPage({super.key});
  static const _levels = <String?>[null, 'Warning', 'Error'];

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Run logs'), actions: [
          IconButton(tooltip: 'Refresh logs', icon: const Icon(Icons.refresh), onPressed: () => context.read<LogsCubit>().load()),
        ]),
        body: BlocBuilder<LogsCubit, LogsState>(builder: (c, s) => Column(children: [
          if (s.summary != null) _SummaryBanner(s.summary!, onTap: (id) { if (id != null) c.read<LogsCubit>().trace(id); }),
          Padding(padding: const EdgeInsets.symmetric(horizontal: 12), child: TextField(
            decoration: const InputDecoration(labelText: 'Search logs', prefixIcon: Icon(Icons.search)),
            onSubmitted: c.read<LogsCubit>().setText)),
          Wrap(spacing: 8, children: [
            for (final l in _levels) ChoiceChip(label: Text(l ?? 'All'), selected: s.level == l, onSelected: (_) => c.read<LogsCubit>().setLevel(l)),
          ]),
          if (s.traceId != null) ListTile(
            title: Text('Tracing request ${s.traceId}'), subtitle: const Text('Showing every log line for this request'),
            trailing: TextButton(onPressed: () => c.read<LogsCubit>().clearTrace(), child: const Text('Show all'))),
          if (s.loading) const LinearProgressIndicator(),
          if (s.error != null) Semantics(liveRegion: true, child: Padding(padding: const EdgeInsets.all(12), child: Text(s.error!))),
          Expanded(child: ListView.builder(itemCount: s.entries.length, itemBuilder: (_, i) => _Row(s.entries[i]))),
        ])),
      );
}

class _SummaryBanner extends StatelessWidget {
  const _SummaryBanner(this.s, {required this.onTap});
  final LogSummary s; final void Function(String?) onTap;
  @override
  Widget build(BuildContext context) => Card(margin: const EdgeInsets.all(12), child: Padding(padding: const EdgeInsets.all(12), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(s.errors == 0 ? 'No errors in the last 24 hours' : '${s.errors} errors in the last 24 hours', style: Theme.of(context).textTheme.titleMedium),
        Text('${s.total} log lines checked'),
        for (final g in s.topErrors.take(3)) ListTile(dense: true, contentPadding: EdgeInsets.zero,
            title: Text(g.signature, maxLines: 2, overflow: TextOverflow.ellipsis), subtitle: Text('${g.count} times'),
            onTap: () => onTap(g.sampleCorrelationId)),
      ])));
}

class _Row extends StatelessWidget {
  const _Row(this.e);
  final LogEntry e;
  @override
  Widget build(BuildContext context) => ListTile(
        leading: Icon(e.isFlaw ? Icons.error_outline : e.level == 'Warning' ? Icons.warning_amber : Icons.check_circle_outline,
            semanticLabel: e.level), // icon + label, never colour alone
        title: Text(e.message, maxLines: 3, overflow: TextOverflow.ellipsis),
        subtitle: Text('${e.time.toLocal().toIso8601String().substring(11, 19)}  ${e.level}${e.exception != null ? '\n${e.exception!.split('\n').first}' : ''}'),
        isThreeLine: e.exception != null,
        onTap: e.correlationId == null ? null : () => context.read<LogsCubit>().trace(e.correlationId!),
      );
}
