# Architecture notes

## Request flow (Hybrid mode)
```mermaid
sequenceDiagram
  participant App as Flutter app
  participant API as .NET API
  participant R as Redis
  participant DB as PostgreSQL
  participant Bus as RabbitMQ / Service Bus
  participant AG as Agent graph
  participant PSP as Gateway
  App->>API: POST /api/payments (Idempotency-Key, JWT)
  API->>R: acquire idempotency lock
  API->>API: fast risk (rules, <= 400 ms)
  alt clear decline
    API->>DB: transaction (Declined) + outbox PaymentDeclined
    API-->>App: 422
  else accepted for deeper check
    API->>DB: transaction + outbox PaymentRequested (one commit)
    API-->>App: 202 Accepted
    DB-->>Bus: outbox relay (SKIP LOCKED)
    Bus->>AG: PaymentRequested
    AG->>DB: AgentAuditLogs (score, reason, model, hash)
    AG->>PSP: route + charge if approved
    PSP-->>DB: settle, outbox PaymentSettled
  end
```

## Security and compliance map
| Concern | Where |
|---|---|
| A01 broken access control | owner checks in handlers and queries; "Ops" role policy on log/audit endpoints |
| A02 crypto failures | pinned TLS in client; Keychain/Keystore; managed identity to Azure OpenAI and Service Bus |
| A03 injection | EF parameterised queries; model output is a whitelisted filter, never SQL |
| A04 insecure design | transactional outbox, idempotency keys, fail-closed options, rate limits |
| A05 misconfiguration | security headers, no stack traces to clients, secrets from env or Key Vault |
| A07 auth failures | JWT validation incl. issuer, audience, lifetime; biometric confirmation per payment |
| A09 logging failures | structured rotating logs, correlation id everywhere, immutable agent audit table |
| PII | pseudonymous owner id; no names or card numbers stored; masking enricher; agents see features only |
| ADA / WCAG | semantic labels, live regions, 48 dp targets, text scaling, icon + text status |

## Failover (infra/azure, infra/aws stubs)
Azure Front Door + Application Gateway front the API. AWS holds a warm standby (API, Postgres read replica via logical replication, Amazon MQ or a shared Service Bus namespace). Front Door health probes shift traffic to the AWS origin when Azure is unhealthy.
