# Should Your Payment Wait for the Fraud Check? Make It a Setting, Not a Debate

*How SentinelPay lets you choose between "check first" and "check after" with one configuration value, without giving up reliability or auditability.*

---

Every payments team eventually has the same argument.

One camp says: **score the payment before you accept it.** If it is fraud, never let it into the system.

The other camp says: **accept it fast and score it afterwards.** Users hate waiting, and your risk model is sometimes slow.

Both camps are right, and both are describing the same trade-off from opposite ends. A slow, thorough check in the request path makes every payment slower and ties your uptime to your model's uptime. A check that only happens afterwards means you have already said "yes" to someone before you know whether you should have.

In SentinelPay, an open reference architecture for mobile payments, I stopped treating this as a debate and made it a setting.

## The idea in one paragraph

Put a **fast, cheap, deterministic check** in the request, with a hard time limit. Write the payment and an event to the database **in one commit** (the transactional outbox). Let a **team of AI agents** do the slower, deeper analysis from the event stream. A single value, `Risk:Mode`, decides how much of the work happens in the request and how much happens afterwards.

| Mode | In the request | After the event |
|---|---|---|
| Off | nothing | routing only |
| Sync | fast score decides, final | nothing |
| Async | nothing | agents decide before any money moves |
| **Hybrid** | fast score within a time limit | clear declines stop at once; low risk skips the deep step; the rest goes to the agents |

Hybrid is the default, and it is where the design gets interesting.

## Why not just call the model in the request?

You can (`SyncUsesAgent: true`), but think about what you are signing up for. A language-model call is slow and variable. If it is in the request path, your payment API now has the latency profile and the outage profile of your model provider.

So the request path uses plain rules: amount tiers plus a per-account velocity counter in Redis. That takes a few milliseconds. It catches the obvious cases (a very large amount, six payments in a minute) and it does so deterministically, which also makes it easy to test and to explain to an auditor.

The expensive reasoning moves out of the request, where a slow answer costs nothing but a little time.

## The part that makes it safe: one commit, two facts

The classic way to lose money in an event-driven system is the dual write. You save the payment, then you publish the event, and the process dies in between. Now the database says "accepted" and the world never heard about it.

SentinelPay writes the transaction **and** its outbox row in the same database commit:

```csharp
db.Transactions.Add(tx);
db.Outbox.Add(OutboxMessage.Create(type, payload));
await db.SaveChangesAsync(ct);   // both, or neither
```

A background relay then moves committed rows to RabbitMQ or Azure Service Bus (a config switch), using `FOR UPDATE SKIP LOCKED` so several API instances can run it without stepping on each other. Delivery is at-least-once, so the consumer is written to be safe on repeats: it checks the transaction's status before every step, and the domain model refuses illegal moves (you cannot route a payment that is not approved).

The client does its part too. Each payment attempt carries an idempotency key, and a retry of the *same* request re-uses the *same* key, so a flaky mobile connection can never charge twice.

## What happens when the fast check is too slow?

You decide, in config:

- `DeferToDeep`: stop waiting, accept, and let the agents decide before any money moves (the default).
- `Decline`: fail closed. Safer, but you will turn away some good payments.
- `Approve`: fail open. Fast, and the one to think hardest about.

Whatever you pick, the timeout itself is written to the audit log. A decision you cannot see is a decision you cannot defend.

## The agents, and how they are kept on a leash

The deep stage is a small graph built with the Microsoft Agent Framework: a feature step computes facts from the account's own history, a behaviour agent and a merchant agent look at the payment in parallel, and an aggregator combines their scores.

Three rules keep this safe enough to put near money:

1. **Agents see features, not people.** The input is amounts, counts and a sanitised merchant ID. No names, no card numbers, no free text. That protects privacy and shrinks the prompt-injection surface.
2. **Replies are parsed strictly.** If a model returns something that is not the expected JSON, the verdict becomes a score of 60, which lands in manual review. Garbage in the reply never becomes an approval.
3. **The model never touches data directly.** The natural-language assistant ("show declined payments over 5,000 last week") turns a question into a whitelisted filter object. The server then runs a parameterised query, forced to the caller's own account. The model cannot write SQL and cannot see anyone else's rows.

Every agent decision lands in an append-only `AgentAuditLogs` table: the score, the reason, the model ID, the latency, and a hash of the inputs. That table is your evidence.

## Seeing what happened

Logs are written as structured JSON in daily rotating files (50 MB cap, 14 kept), and every line carries a correlation ID that the mobile app generated. The same ID appears on the API request, the outbox event, the agent audit rows and the log lines.

In the app, an operations user opens **Run logs**, sees a summary ("3 errors in the last 24 hours, grouped by cause"), taps a failing entry, and sees every line for that one request. Normal runs and flaws are the same screen, filtered differently.

## What I have and have not measured

Honesty matters more than a good chart, so here it is plainly: the scaffold has unit and integration tests for the behaviour (every mode, the timeout paths, duplicate requests, access control), but I have **not** yet run load or fraud-accuracy experiments. The accompanying paper defines the experiments and leaves the results blank until they exist. The Agent Framework portion is also written against a preview API and needs to be compiled and adjusted against the version you install.

## Take-aways

- Treat "check before or after" as a **configuration**, not an architecture war.
- Keep the in-request check **fast, deterministic and time-boxed**.
- Never dual-write: use the **outbox**.
- Give AI agents **features, strict output parsing and no direct data access**.
- Log every decision somewhere **append-only**, tied together by one **correlation ID**.

The full reference implementation (Flutter client, .NET 9 backend, Docker Compose, tests) and the formal paper are linked below.

*[Link to GitHub repository]  ·  [Link to paper]*

---

*Diagrams to add before publishing: export diagrams 3 (create payment), 4 (outbox to settlement) and 7 (risk path) from the diagram page as images. Medium does not render Mermaid.*
