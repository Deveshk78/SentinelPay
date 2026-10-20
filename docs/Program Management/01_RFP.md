# Request for Proposal: Delivery of the SentinelPay Payments Platform

**Issuer:** [Company name]  **RFP ref:** [SP-RFP-001]  **Issue date:** [date]  **Proposals due:** [date + 3 weeks]  **Contact:** [name, email]

## 1. Purpose
[Company] invites qualified firms to propose delivery of SentinelPay: an event-driven mobile payment platform with a Flutter client, a .NET 9 backend, and an AI agent risk engine. A reference scaffold and architecture documents exist; the selected partner will complete, harden, test and release the product in **six months**.

## 2. Background
The scaffold contains the domain model (accounts, transactions, agent audit logs, outbox), a configurable risk mode (Off, Sync, Async, Hybrid), RabbitMQ and Azure Service Bus adapters, Redis idempotency, rotating logs with an in-app viewer, and unit and integration test suites. It has not yet been built or load-tested.

## 3. Scope of work
1. Compile, stabilise and complete the scaffold, including the Microsoft Agent Framework integration.
2. Replace sandbox components: payment gateway adapters ([list PSPs]), identity provider integration, Azure Front Door and Application Gateway, AWS warm-standby failover (infrastructure as code).
3. Security hardening to the OWASP Top 10, independent penetration test, remediation.
4. Privacy and compliance readiness for PII handling, PCI DSS scope reduction, and accessibility (WCAG 2.2 AA).
5. Performance, resilience and fault-injection testing against the targets in Section 5.
6. Customer-ready product documentation, runbooks, and administrator training.
7. Pilot support through go-live.

Out of scope: card-network certification, issuing, KYC provider selection, marketing.

## 4. Deliverables
Production-ready source and pipelines; infrastructure as code for Azure and AWS; test reports; penetration test report and closure evidence; compliance evidence pack; operations runbooks; product documentation; knowledge-transfer sessions.

## 5. Key requirements (summary of document 02)
| Area | Target |
|---|---|
| Acceptance latency, Hybrid mode | p95 at most [300] ms added by risk scoring |
| Availability | [99.9]% monthly, regional failover RTO [15] min, RPO [1] min |
| Integrity | zero duplicate financial effects and zero lost accepted payments under the fault tests |
| Security | no open high or critical findings at release |
| Accessibility | WCAG 2.2 AA on the mobile client |
| Auditability | every agent decision stored with score, reason, model, input hash |

## 6. Schedule
Kick-off [date]; month-3 checkpoint; month-5 security freeze; month-6 go-live. Detail in document 05.

## 7. Proposal format (maximum 30 pages plus appendices)
Executive summary; understanding of the work; technical approach; team and CVs (named key personnel); plan against the six months; quality and security approach; risk view; three comparable references; fixed-price or capped time-and-materials fee table; assumptions and exclusions.

## 8. Evaluation
| Criterion | Weight |
|---|---|
| Technical approach and understanding | 30% |
| Relevant payments, security and .NET/Flutter experience | 25% |
| Team quality and key-person commitment | 15% |
| Plan, risk management, quality approach | 10% |
| Price and commercial terms | 20% |

Shortlisted firms present and run a two-hour technical working session on the scaffold.

## 9. Commercial terms
Milestone-based payments tied to acceptance; [10]% retention until the pilot is stable; IP in all deliverables assigned to [Company]; confidentiality and data-protection agreement before access to any data; right to audit; no use of [Company] data to train third-party models; subcontracting only with written consent.

## 10. Process
Questions by [date], answers shared with all bidders. Proposals by [date]. Clarifications and presentations [dates]. Award [date]. [Company] may reject any or all proposals and is not liable for bid costs.
