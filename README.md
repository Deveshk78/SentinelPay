# SentinelPay

Event-driven mobile payments: Flutter client, .NET 9 backend, and a multi-agent risk engine (Microsoft Agent Framework).

```
client/    Flutter (Clean Architecture, BLoC, pinned TLS, biometrics, secure storage)
backend/   .NET 9: Domain -> Application -> Infrastructure -> Api, plus Agents (isolated)
infra/     Dockerfile, Azure + AWS stubs
scripts/   bootstrap.sh, audit_immutable.sql
docs/      ARCHITECTURE.md
```

## Run it
```bash
cp .env.example .env            # set strong values
docker compose up -d            # PostgreSQL, Redis, RabbitMQ
./scripts/bootstrap.sh          # solution, restore, build, unit tests
cd backend/src/SentinelPay.Api && dotnet run
cd client && flutter pub get && flutter test
```
Integration tests (`dotnet test backend/tests/SentinelPay.Integration.Tests`) start their own PostgreSQL and Redis with Testcontainers; Docker must be running.

## Risk modes (configuration, no redeploy of code)
Set `Risk:Mode`. It is hot-reloadable (`IOptionsMonitor`).

| Mode | In the request | After the outbox (agents) | Use when |
|---|---|---|---|
| `Off` | nothing | routing only | load tests, sandbox |
| `Sync` | fast score decides; final | none | strict "decide before accept" rules |
| `Async` | nothing | deep agent graph decides before routing | lowest latency for the caller |
| `Hybrid` (default) | fast score within `SyncTimeoutMs` | clear declines stop at once; score < `SkipDeepBelowScore` skips deep; the rest, and anything over `AlwaysDeepAbove`, goes deep | recommended |

If the fast check times out, `OnSyncTimeout` decides: `DeferToDeep` (default), `Decline` (fail closed), or `Approve` (fail open).
In every mode the transaction and its outbox event are written in one commit, and funds only move after routing.

## Where things are
- Entities: `backend/src/SentinelPay.Domain/Entities` (Account, Transaction, AgentAuditLog, OutboxMessage)
- Payment flow: `Application/Payments/CreatePaymentHandler.cs` (request side), `PaymentProcessingHandler.cs` (consumer side)
- Agents: `SentinelPay.Agents/RiskGraph.cs` (graph), `QuerySpecParser.cs` (natural-language search)
- Broker switch: `Messaging:Provider` = `RabbitMq` | `ServiceBus`
- Rotating logs: `Api/Program.cs` (writer), `Infrastructure/Logging/LogQueryService.cs` (reader), `client/lib/features/ops_logs` (viewer)

## Read this before production
1. **Agents project is the one unverified part.** The Microsoft Agent Framework was moving between previews; `RiskGraph.cs` uses the workflow API from memory. Restore, compile, and adjust names. Nothing else depends on it (port `IAgentRiskGraph`).
2. I could not build or run anything in this environment (no network). Run `bootstrap.sh` first and fix any compile errors; package versions in `Directory.Packages.props` are starting points.
3. Replace the symmetric JWT key with your identity provider (Entra ID or similar). Replace `SandboxGatewayClient` with real PSP adapters.
4. Fill in the certificate pins in `client/lib/core/config/app_config.dart`.
5. Run `scripts/audit_immutable.sql` after the first migration (append-only audit table).
6. Use EF migrations, not `EnsureCreated`, outside Development.

## Tests
- Unit: `dotnet test backend/tests/SentinelPay.Domain.Tests`, `...Application.Tests`, `...Agents.Tests`
- Integration (needs Docker): `dotnet test backend/tests/SentinelPay.Integration.Tests`
- Client: `cd client && flutter test` (includes accessibility tests)
- Load and fault injection: `tests/load/payments.js` (k6) and `tests/fault/README.md`
- What was found while writing them: `docs/TEST_FINDINGS.md`
