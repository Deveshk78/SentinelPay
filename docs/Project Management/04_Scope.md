# Project Scope Statement and WBS

## 1. Product scope
A mobile payment platform: Flutter client; .NET 9 API with Clean Architecture; PostgreSQL and Redis; RabbitMQ or Service Bus; a risk engine with four configurable modes; a Microsoft Agent Framework graph for deep risk analysis, gateway routing support and natural-language transaction search; rotating logs with an in-app viewer; Azure hosting with AWS failover; security, privacy and accessibility controls; customer documentation.

## 2. Deliverables and acceptance
| # | Deliverable | Acceptance criterion |
|---|---|---|
| D1 | Backend v1.0 | all M requirements in document 02 pass; CI green |
| D2 | Mobile client v1.0 (iOS, Android) | store-ready builds; WCAG 2.2 AA audit passed |
| D3 | Agent engine | decisions audited; fail-toward-review verified by tests |
| D4 | Infrastructure as code (Azure, AWS) | failover drill meets RTO and RPO |
| D5 | Test evidence | unit, integration, load, fault-injection reports |
| D6 | Security evidence | independent pen-test; no open high or critical findings |
| D7 | Compliance pack | privacy, security, audit documents complete (later deliverables) |
| D8 | Product and operations documentation | reviewed by a pilot customer |
| D9 | Pilot | one customer live with agreed success criteria |

## 3. Exclusions
Card-network certification or acquiring licences; KYC and sanctions screening providers; issuing; marketing; refunds and reversals, manual review queue and admin console (listed as FR-13 to FR-15 for release 1.1 unless re-prioritised); on-premises deployment.

## 4. Assumptions
Pilot customer available from week 22. Gateway sandbox access by week 5. Azure and AWS subscriptions provisioned by week 2. Agent Framework API usable at the version restored in week 1, or a fallback agreed.

## 5. Constraints
Six months; the funding in document 06; data residency [region]; no card numbers stored by the platform.

## 6. Change control
Anyone raises a change request; the program manager assesses cost, schedule, risk within 3 working days; changes under 1 week or $10,000 approved by the program manager if inside contingency; larger changes go to the sponsor. All changes are logged and the baseline re-issued.

## 7. Work breakdown structure
```
1 Management            1.1 Mobilise  1.2 Governance and reporting  1.3 Change control  1.4 Close-out
2 Architecture          2.1 Finalise design  2.2 Agent Framework spike  2.3 Threat model
3 Backend               3.1 Payment flow  3.2 Outbox and brokers  3.3 Idempotency  3.4 Gateway adapters  3.5 Auth and identity
4 Risk and agents       4.1 Fast scorer  4.2 Agent graph  4.3 Query agent  4.4 Audit store  4.5 Mode and timeout logic
5 Client                5.1 Payment flow  5.2 Security (pinning, biometrics, storage)  5.3 Assistant  5.4 Log viewer  5.5 Accessibility
6 Infrastructure        6.1 CI/CD  6.2 Azure edge and hosting  6.3 AWS failover  6.4 Observability and log shipping
7 Quality               7.1 Unit  7.2 Integration  7.3 Load  7.4 Fault injection  7.5 Accessibility audit
8 Security & compliance 8.1 Hardening  8.2 Pen-test  8.3 Privacy (DPIA)  8.4 Evidence pack
9 Documentation         9.1 Product docs  9.2 Runbooks  9.3 Training
10 Release              10.1 UAT  10.2 Pilot  10.3 Go-live  10.4 Hypercare
```
