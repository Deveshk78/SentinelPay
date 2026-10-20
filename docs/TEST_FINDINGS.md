# Findings from writing the missing tests

Writing tests for the gaps listed in the audit pack surfaced these. **None of the tests have been executed** (no .NET, Flutter or Docker in the authoring environment), so treat every test as unproven until CI runs it.

## Fixed in code
1. **Charge-before-debit ordering (money risk).** `PaymentProcessingHandler` charged the gateway first and debited the account afterwards. If the debit then failed (balance changed since acceptance), the customer would have been charged with no ledger entry. Now funds are reserved (debited) first, committed with the routed status, and credited back if the charge fails; an unaffordable payment fails without ever calling the gateway. Covered by `Declined_gateway_charge_releases_the_reserved_funds` and `Insufficient_funds_fails_without_ever_calling_the_gateway`.
2. **Log masking now scrubs e-mail addresses** in any string property, in addition to sensitive property names and card-number-shaped strings.

## Still open
3. **Gateway exception leaves funds reserved.** If `ChargeAsync` throws (timeout, unknown outcome) the transaction stays `Routed` with funds reserved and a redelivered message returns early. Needs gateway-side idempotency keys and a reconciliation job (fault runbook F6 and F10, query 4).
4. **Exception text and rendered messages are not scrubbed** (one test is skipped to mark this).
5. **Possible migration issue:** the `xmin` concurrency token is mapped as a column. Npgsql normally treats it as a system column and does not create it, but verify with the first migration and with `EnsureCreated` in the integration tests.
6. **Manual review queue (FR-13) is still unbuilt**; held payments have no resolution path.
7. **Redis outage behaviour is unspecified.** The idempotency lock and velocity counter both depend on Redis; decide whether to fail closed, and test (F8).
8. **200% text layout** on the log screen may overflow; the widget test will tell.

## What the suite now covers
| Area | Tests |
|---|---|
| Domain state machine | `TransactionTests`, `AccountTests` |
| Request-side risk modes, timeouts, idempotency, ownership | `CreatePaymentHandlerTests` (10) |
| Consumer: settle, refund, redelivery, deep decline/review/approve | `PaymentProcessingHandlerTests` (11) |
| Assistant scoping, clamping, input limits | `AssistantQueryServiceTests` (7) |
| Agent fallback and cancellation | `RiskScoringRouterTests` (9) |
| Model-reply hardening and prompt-input sanitising | `AgentBoundaryTests` |
| Log reader | `LogQueryServiceTests` |
| Outbox relay on real PostgreSQL | `OutboxRelayTests` |
| Audit table immutability | `AuditTriggerTests` |
| JWT, headers, correlation id, rate limit, duplicate-request race | `SecurityTests` |
| PII masking | `PiiMaskingTests` |
| API happy and error paths | `PaymentApiTests` |
| Client bloc and cubit | `payment_bloc_test`, `logs_cubit_test` |
| Client accessibility | `accessibility/*` |
| Load and fault injection | `tests/load/payments.js`, `tests/fault/README.md` (manual or scripted runs) |
