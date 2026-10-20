# Fault-injection runbook

Run in staging with the Docker Compose stack (or the staging equivalent). For every scenario: inject, send traffic (use `tests/load/payments.js` at a modest rate), remove the fault, wait 2 minutes, then run the **verification queries** below. File the output as evidence (doc 14, `/fault-injection`).

| # | Fault | How to inject | Expected behaviour |
|---|---|---|---|
| F1 | Broker down | `docker compose stop rabbitmq` for 5 min, then start | payments still accepted (202); outbox rows pile up; after restart all are delivered once |
| F2 | Consumer crash mid-processing | `docker compose kill api` during load, then start | no duplicate charges; Approved/PendingRisk payments are completed after restart |
| F3 | Duplicate delivery | republish a PaymentRequested from the management UI | no second charge, no second ledger movement |
| F4 | Client retry storm | repeat one request 50 times in parallel with one Idempotency-Key | exactly one transaction |
| F5 | Gateway returns failure | set the sandbox gateway to fail | funds released, status Failed, no Settled event |
| F6 | Gateway times out (unknown outcome) | make the gateway hang past its timeout | **known gap**: transaction stays Routed with funds reserved; reconciliation must find and resolve it |
| F7 | Model timeout or malformed reply | point the agent at a stub that sleeps or returns text | fast path unaffected; deep stage falls back to rules or review; never an approval from garbage |
| F8 | Redis down | `docker compose stop redis` | requests fail safely (no double charge); recovery without data loss; record the actual behaviour |
| F9 | Database failover | trigger failover to the replica / AWS standby | RTO and RPO within targets; no lost accepted payment |
| F10 | Kill between charge and persist | kill the process right after the gateway call (use a debugger hook or chaos tool) | **known gap**, same as F6 |

## Verification queries (all should return zero rows or zero)
```sql
-- 1. duplicate financial effects
SELECT "AccountId","IdempotencyKey",count(*) FROM transactions GROUP BY 1,2 HAVING count(*) > 1;

-- 2. accepted payment with no event (lost acceptance)
SELECT count(*) FROM transactions t
WHERE NOT EXISTS (SELECT 1 FROM outbox_messages o WHERE o."Payload"->>'TransactionId' = t."Id"::text);

-- 3. events never delivered after recovery
SELECT count(*) FROM outbox_messages WHERE "ProcessedAt" IS NULL AND "Attempts" < 10 AND "CreatedAt" < now() - interval '5 minutes';

-- 4. stuck money (reserved but unresolved)
SELECT "Id","AccountId","Amount","UpdatedAt" FROM transactions WHERE "Status" = 'Routed' AND "UpdatedAt" < now() - interval '5 minutes';

-- 5. settled without an approval decision (invariant I1)
SELECT "Id" FROM transactions WHERE "Status" IN ('Routed','Settled') AND "RiskDecision" IS DISTINCT FROM 'Approve';

-- 6. ledger check: no negative balances
SELECT "Id","Balance" FROM accounts WHERE "Balance" < 0;
```
Query 4 returning rows after F6 or F10 is the expected, documented gap; it must be zero before go-live once gateway-side idempotency and the reconciliation job exist.
