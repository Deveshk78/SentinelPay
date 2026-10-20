# Requirements Document

Priority: **M** must, **S** should, **C** could. IDs are stable so tests and audits can trace to them. The "Scaffold" column shows where the current code already addresses it.

## 1. Business requirements
| ID | Requirement | Pri | Success measure |
|---|---|---|---|
| BR-1 | Let customers choose where risk scoring sits (before accept, after, or hybrid) without a code change | M | Mode change by configuration only |
| BR-2 | Reach general availability in 6 months | M | Go-live by week 26 |
| BR-3 | Be sellable to regulated buyers (banks, fintechs) | M | Audit pack and product documentation delivered |
| BR-4 | Reach payback within [18] months of project start | S | See document 06 |

## 2. Functional requirements
| ID | Requirement | Pri | Scaffold |
|---|---|---|---|
| FR-1 | Create a payment with an idempotency key; repeated requests return the original result | M | CreatePaymentHandler |
| FR-2 | Verify the caller owns the account before any action | M | owner checks |
| FR-3 | Fast risk check with configurable timeout and timeout policy | M | RiskOptions |
| FR-4 | Hybrid decision: decline at once, skip deep for low risk, defer the rest | M | CreatePaymentHandler |
| FR-5 | Commit transaction and outbox event atomically; relay to broker | M | OutboxRelay |
| FR-6 | Deep risk analysis by agent graph before routing | M | RiskGraph |
| FR-7 | Select a payment gateway by policy; charge; settle; handle failure | M | gateway router |
| FR-8 | Record every agent decision in an append-only audit store | M | AgentAuditLog, SQL trigger |
| FR-9 | Natural-language transaction search limited to the caller's account | S | AssistantQueryService |
| FR-10 | Broker selectable: RabbitMQ or Azure Service Bus | M | MessagingOptions |
| FR-11 | Mobile payment with biometric confirmation | M | BiometricGate |
| FR-12 | Operations log viewer: filter, summary of errors, trace by correlation id | M | LogQueryService, logs screen |
| FR-13 | Manual review queue for held payments | S | [not built] |
| FR-14 | Refunds and reversals | S | [not built] |
| FR-15 | Customer admin console for risk thresholds and gateways | C | [not built] |

## 3. Non-functional requirements
| ID | Requirement | Target |
|---|---|---|
| NFR-1 | Added latency from risk in Hybrid | p95 at most [300] ms |
| NFR-2 | Availability | [99.9]% monthly |
| NFR-3 | Failover Azure to AWS | RTO [15] min, RPO [1] min |
| NFR-4 | Throughput | [200] payments per second sustained per region |
| NFR-5 | Integrity | 0 duplicate effects, 0 lost accepted payments under fault tests |
| NFR-6 | Security | OWASP Top 10 controls verified; no open high or critical findings at release |
| NFR-7 | Privacy | no names or card numbers stored; logs mask PII; agents see features only |
| NFR-8 | Accessibility | WCAG 2.2 AA; screen-reader, text scaling, 48 dp targets |
| NFR-9 | Observability | correlation id on every request, event, audit row and log line |
| NFR-10 | Log retention | 14 daily files minimum on disk; longer retention via log shipping [90 days] |
| NFR-11 | Maintainability | Clean Architecture layers; agent project isolated behind an interface |
| NFR-12 | Test coverage | unit tests on domain and application logic; integration tests on the API |

## 4. Constraints and assumptions
Azure primary, AWS failover. PostgreSQL, Redis, RabbitMQ or Service Bus. Microsoft Agent Framework (preview API risk). Card data stays with the payment gateway (PCI scope reduction). Rates, volumes and targets in brackets are to be confirmed.

## 5. Acceptance
A requirement is accepted when its test passes in the release candidate and the evidence is filed in the compliance pack.
