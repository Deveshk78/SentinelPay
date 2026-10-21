# SentinelPay: class, sequence and workflow diagrams

Every name below comes from the scaffold in `sentinelpay-scaffold.zip`. Each block is plain Mermaid, so you can paste it into GitHub, Medium (via image export) or an IEEE figure.

## 1. Class diagram: domain model

```mermaid
classDiagram
  class Entity {
    <<abstract>>
    +Guid Id
    +DateTimeOffset CreatedAt
    +DateTimeOffset UpdatedAt
  }
  class Account {
    +Guid OwnerId
    +string Currency
    +decimal Balance
    +AccountStatus Status
    +Open(ownerId, currency, opening)$ Account
    +CanAfford(amount, currency) bool
    +Debit(amount)
    +Credit(amount)
    +Freeze()
  }
  class Transaction {
    +Guid AccountId
    +decimal Amount
    +string Currency
    +string MerchantId
    +TransactionStatus Status
    +int RiskScore
    +RiskDecision RiskDecision
    +RiskStage RiskStage
    +string Gateway
    +string IdempotencyKey
    +string CorrelationId
    +Create(...)$ Transaction
    +ApplyRisk(score, decision, stage)
    +MarkRouted(gateway)
    +MarkSettled()
    +MarkFailed()
  }
  class AgentAuditLog {
    +string CorrelationId
    +Guid TransactionId
    +string AgentName
    +RiskStage Stage
    +string InputHash
    +RiskDecision Decision
    +int Score
    +string Rationale
    +string ModelId
    +long LatencyMs
    +bool TimedOut
    +Record(...)$ AgentAuditLog
  }
  class OutboxMessage {
    +string Type
    +string Payload
    +DateTimeOffset ProcessedAt
    +int Attempts
    +string Error
    +Create(type, payload)$ OutboxMessage
    +MarkProcessed()
    +MarkFailed(error)
  }
  class IIntegrationEvent {
    <<interface>>
    +Guid TransactionId
    +string CorrelationId
  }
  class PaymentRequested { +bool DeepRiskRequired }
  class PaymentDeclined { +string Reason }
  class PaymentRouted { +string Gateway }
  class PaymentSettled
  Entity <|-- Account
  Entity <|-- Transaction
  Entity <|-- AgentAuditLog
  Entity <|-- OutboxMessage
  IIntegrationEvent <|.. PaymentRequested
  IIntegrationEvent <|.. PaymentDeclined
  IIntegrationEvent <|.. PaymentRouted
  IIntegrationEvent <|.. PaymentSettled
  Account "1" --> "*" Transaction : AccountId
  Transaction "1" --> "*" AgentAuditLog : TransactionId
```

## 2. Class diagram: application, ports and adapters

```mermaid
classDiagram
  direction LR
  class CreatePaymentHandler { +HandleAsync(CreatePaymentCommand) PaymentResult }
  class PaymentProcessingHandler { +HandleAsync(PaymentRequested) }
  class AssistantQueryService { +AskAsync(accountId, ownerId, question) AssistantAnswer }
  class RiskOptions {
    +RiskMode Mode
    +int SyncTimeoutMs
    +TimeoutAction OnSyncTimeout
    +int ReviewThreshold
    +int DeclineThreshold
    +int SkipDeepBelowScore
    +decimal AlwaysDeepAbove
    +bool SyncUsesAgent
  }
  class IAppDbContext { <<port>> }
  class IRiskScoringService { <<port>> +ScoreAsync(ctx, stage) RiskAssessment }
  class IIdempotencyStore { <<port>> +TryAcquireAsync(key, ttl) bool }
  class IEventPublisher { <<port>> +PublishAsync(type, payload, correlationId) }
  class IPaymentGatewayRouter { <<port>> +SelectAsync(tx) GatewayChoice }
  class IGatewayClient { <<port>> +ChargeAsync(gateway, tx) bool }
  class IAgentRiskGraph { <<port>> +RunAsync(ctx) RiskAssessment }
  class IQuerySpecParser { <<port>> +ParseAsync(question, now) TransactionQuerySpec }

  class AppDbContext
  class OutboxRelay { <<BackgroundService>> }
  class RabbitMqEventPublisher
  class ServiceBusEventPublisher
  class RabbitMqConsumer { <<BackgroundService>> }
  class ServiceBusConsumer { <<BackgroundService>> }
  class RedisIdempotencyStore
  class RiskScoringRouter
  class RuleBasedRiskScorer
  class RuleBasedGatewayRouter
  class SandboxGatewayClient
  class LogQueryService
  class RiskGraph { <<Agent Framework graph>> }
  class QuerySpecParser { <<Agent>> }

  CreatePaymentHandler --> IAppDbContext
  CreatePaymentHandler --> IRiskScoringService
  CreatePaymentHandler --> IIdempotencyStore
  CreatePaymentHandler --> RiskOptions
  PaymentProcessingHandler --> IAppDbContext
  PaymentProcessingHandler --> IRiskScoringService
  PaymentProcessingHandler --> IPaymentGatewayRouter
  PaymentProcessingHandler --> IGatewayClient
  AssistantQueryService --> IQuerySpecParser
  AssistantQueryService --> IAppDbContext

  IAppDbContext <|.. AppDbContext
  IEventPublisher <|.. RabbitMqEventPublisher
  IEventPublisher <|.. ServiceBusEventPublisher
  IIdempotencyStore <|.. RedisIdempotencyStore
  IRiskScoringService <|.. RiskScoringRouter
  RiskScoringRouter --> RuleBasedRiskScorer : fast path
  RiskScoringRouter --> IAgentRiskGraph : deep path
  IAgentRiskGraph <|.. RiskGraph
  IQuerySpecParser <|.. QuerySpecParser
  IPaymentGatewayRouter <|.. RuleBasedGatewayRouter
  IGatewayClient <|.. SandboxGatewayClient
  OutboxRelay --> IEventPublisher
  RabbitMqConsumer --> PaymentProcessingHandler
  ServiceBusConsumer --> PaymentProcessingHandler
```

## 3. Sequence: create a payment (Hybrid mode)

```mermaid
sequenceDiagram
  autonumber
  participant App as Flutter app
  participant API as API and CreatePaymentHandler
  participant R as Redis
  participant Rules as RuleBasedRiskScorer
  participant DB as PostgreSQL
  App->>App: biometric confirm, new Idempotency-Key
  App->>API: POST /api/payments (JWT, key, correlation id)
  API->>API: validate input
  API->>DB: look up existing transaction for key
  API->>R: SET idem:{account}:{key} NX
  API->>DB: load account (owner must match)
  API->>Rules: ScoreAsync(SyncFast) with timeout
  Rules->>R: INCR vel:{account} (60 s window)
  alt score at or above DeclineThreshold
    API->>DB: Transaction Declined, audit row, outbox PaymentDeclined
    API-->>App: 422 Payment declined
  else timeout
    API->>DB: audit row TimedOut
    Note over API: OnSyncTimeout decides: DeferToDeep, Decline or Approve
    API-->>App: 202 Accepted (risk checks in progress)
  else low score and amount at or below AlwaysDeepAbove
    API->>DB: Transaction Approved, outbox PaymentRequested deep=false
    API-->>App: 202 Accepted
  else everything else
    API->>DB: Transaction PendingRisk, outbox PaymentRequested deep=true
    API-->>App: 202 Accepted
  end
```

## 4. Sequence: outbox, agents, gateway, settlement

```mermaid
sequenceDiagram
  autonumber
  participant DB as PostgreSQL
  participant Relay as OutboxRelay
  participant Bus as RabbitMQ or Service Bus
  participant Cons as Consumer
  participant H as PaymentProcessingHandler
  participant G as RiskGraph (agents)
  participant PSP as Payment gateway
  loop every 500 ms
    Relay->>DB: SELECT unprocessed FOR UPDATE SKIP LOCKED
    Relay->>Bus: publish PaymentRequested
    Relay->>DB: mark processed
  end
  Bus->>Cons: PaymentRequested (at least once)
  Cons->>H: HandleAsync
  H->>DB: load transaction (skip if already handled)
  opt DeepRiskRequired
    H->>G: RunAsync(features only)
    G->>G: FeatureExecutor, then Behaviour and Merchant agents in parallel, then Aggregator
    G-->>H: score, rationale, input hash
    H->>DB: AgentAuditLogs row, ApplyRisk(Deep)
  end
  alt Declined or RiskReview
    H->>DB: outbox PaymentDeclined (decline) or hold for review
  else Approved
    H->>PSP: ChargeAsync(chosen gateway)
    H->>DB: MarkRouted, debit account, MarkSettled, outbox PaymentSettled
  end
  Note over Cons,Bus: on failure the message is retried once, then dead-lettered
```

## 5. Sequence: natural-language query and log trace

```mermaid
sequenceDiagram
  autonumber
  actor User
  participant App as Flutter app
  participant API
  participant Q as QuerySpecParser (agent)
  participant DB as PostgreSQL
  participant Logs as LogQueryService
  User->>App: "declined payments over 5000 last week"
  App->>API: POST /api/assistant/query
  API->>DB: confirm caller owns the account
  API->>Q: question only, no account data
  Q-->>API: TransactionQuerySpec (whitelisted filter)
  API->>DB: parameterised query, forced to this account
  API-->>App: summary and rows
  User->>App: open Run logs, tap an error
  App->>API: GET /api/ops/logs?correlationId=...
  API->>Logs: read rolling NDJSON files
  Logs-->>App: every log line for that request
```

## 6. Workflow: transaction lifecycle

```mermaid
stateDiagram-v2
  [*] --> PendingRisk: Create
  PendingRisk --> Approved: ApplyRisk Approve
  PendingRisk --> RiskReview: ApplyRisk Review
  PendingRisk --> Declined: ApplyRisk Decline
  Approved --> RiskReview: deep stage downgrades
  Approved --> Declined: deep stage declines
  Approved --> Routed: MarkRouted
  Routed --> Settled: gateway ok and debit ok
  Routed --> Failed: gateway or debit fails
  RiskReview --> [*]: manual review
  Declined --> [*]
  Settled --> [*]
  Failed --> [*]
```

## 7. Workflow: choosing the risk path

```mermaid
flowchart TD
  A["Payment request"] --> B{"Risk mode"}
  B -->|Off| C["Approve, no scoring"]
  B -->|Async| H["Pending, deep stage required"]
  B -->|Sync| D["Fast score with timeout"]
  B -->|Hybrid| D
  D --> E{"Timed out?"}
  E -->|yes| F{"OnSyncTimeout"}
  F -->|Decline| X["Declined"]
  F -->|Approve| C
  F -->|DeferToDeep| G{"Mode"}
  G -->|Hybrid| H
  G -->|Sync| R["Review"]
  E -->|no| I{"Score vs thresholds"}
  I -->|"decline or Sync mode"| J["Final decision now"]
  I -->|"low score and normal amount"| K["Approved, skip deep"]
  I -->|"otherwise (Hybrid)"| H
  H --> L["Agent graph after outbox"]
  L --> M{"Decision"}
  M -->|Approve| N["Route and settle"]
  M -->|Review| R
  M -->|Decline| X
  C --> N
  K --> N
  J --> N
```

## 8. Workflow: agent graph

```mermaid
flowchart LR
  In["RiskContext (no PII)"] --> F["FeatureExecutor: 24h count, 30d average and max, new merchant"]
  F --> B["BehaviourAgent"]
  F --> M["MerchantAgent"]
  B --> A["AggregatorExecutor: 0.6 max plus 0.4 average"]
  M --> A
  A --> Out["RiskAssessment, then AgentAuditLogs row"]
  B -. "unparseable reply scores 60" .-> A
  M -. "unparseable reply scores 60" .-> A
```