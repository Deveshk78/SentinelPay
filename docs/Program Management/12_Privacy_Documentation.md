# Privacy Documentation

**Status:** design-level draft prepared from the scaffold. It is not legal advice and not a certification. Have counsel and a data-protection officer review it before use, and confirm which laws apply to your customers.

## 1. Scope and roles
SentinelPay is a processing platform. For each customer (bank, fintech) the customer is normally the **controller** and SentinelPay (the operator of the hosted service) is the **processor**; if you operate the service directly for end users you are the controller. Settle this in each customer contract. Frameworks considered: EU GDPR (Regulation 2016/679), India's Digital Personal Data Protection Act 2023, and payment-card rules (PCI DSS v4.0). Confirm the current in-force status of rules under the DPDP Act with counsel.

## 2. Data inventory (what the scaffold stores)
| Data | Where | Personal data? | Class | Notes |
|---|---|---|---|---|
| Account owner identifier (`OwnerId`) | accounts | pseudonymous | Confidential | a random GUID; the link to a real person is held by the customer's identity system |
| Balance, currency, status | accounts | financial | Confidential | |
| Amount, currency, merchant ID, status, risk score, gateway | transactions | financial, behavioural | Confidential | merchant ID is restricted to letters, digits, `-`, `_` |
| Idempotency key, correlation ID | transactions, logs | technical identifiers | Internal | |
| Agent decisions: score, reason, model ID, latency, input hash | agent_audit_logs | derived | Restricted | hash, not raw features |
| Event payloads | outbox_messages | identifiers only | Internal | transaction ID, correlation ID, flags |
| Access token (JWT) | device secure storage | credential | Restricted | Keychain/Keystore only |
| Operational logs | rolling files | should contain none | Internal | masking applied; see gaps |
| Names, emails, phone numbers, addresses, card numbers | **not stored** | | | by design |

**Not collected:** card numbers (held by the payment gateway), government IDs, location, contacts, advertising identifiers.

## 3. Purposes and lawful bases (to confirm per customer)
| Purpose | Typical basis under GDPR | Notes |
|---|---|---|
| Executing the payment | contract (Art. 6(1)(b)) | |
| Fraud prevention and risk scoring | legitimate interest (Art. 6(1)(f)) and legal obligation where applicable | document a balancing test; fraud prevention is recognised in recitals |
| Audit and security logging | legitimate interest; legal obligation | |
| Product analytics | **not included**; add only with a separate basis | |
Under the DPDP Act the position depends on consent and the "legitimate uses" provisions; the customer's counsel decides which applies.

## 4. Automated decisions
Declining a payment on a risk score is an automated decision with significant effect. GDPR Art. 22 restricts decisions based solely on automated processing and requires safeguards. The design supports safeguards: a **Review** outcome that routes to a person (the review queue must exist before go-live, see document 11), a stored **rationale** for each decision, and a documented path for a customer to contest a decline. Agents only produce a score and reason; thresholds are deterministic and set by configuration.

## 5. Privacy by design: how the scaffold implements it
| Principle | Implementation |
|---|---|
| Data minimisation | no names or card numbers; agents receive amounts, counts, aggregates and a sanitised merchant ID only |
| Purpose limitation | agent audit holds hashes and reasons, not raw inputs; logs carry correlation IDs, not identities |
| Storage limitation | retention schedule below; 14 daily log files kept on disk by default |
| Integrity and confidentiality | TLS with pinning on mobile; encryption at rest (database, backups, disks); managed identities; role-based access to logs and audit |
| Accountability | append-only audit table; correlation IDs; records of processing; change control on prompts and thresholds |
| Transparency | privacy notice from the controller; plain-language decline messages in the client |

## 6. Retention schedule (proposed; customer to confirm)
| Data | Retention | Then |
|---|---|---|
| Transactions and agent audit | [7 years] or the financial-records period that applies | archive, then delete |
| Outbox rows | [30 days] after processing | delete |
| Application logs on disk | 14 days; central store [90 days] | delete |
| Idempotency keys (Redis) | 10 minutes lock; DB key kept with the transaction | |
| Backups | [35 days] | expire |
Deletion requests: transactions that a legal duty requires you to keep are retained and restricted; everything else is erased or irreversibly de-linked by removing the owner link in the customer's identity system.

## 7. Data-subject rights procedure
Requests arrive through the controller. Steps: verify identity; locate data by `OwnerId` (accounts, transactions, audit); export in a machine-readable format (access, portability); correct; restrict or erase where permitted; respond within the legal period ([30 days] under GDPR, extendable); log the request. Build a small internal tool for export and erase in release 1.1 if volume warrants.

## 8. Processors, sub-processors and transfers
| Provider | Purpose | Data | Region (confirm) |
|---|---|---|---|
| Microsoft Azure | hosting, Service Bus, Key Vault | all platform data | [region] |
| Azure OpenAI | agent analysis | features only | [region]; confirm data-handling and no-training terms |
| AWS | warm standby and replication | replicated platform data | [region] |
| Payment gateway(s) | charging | amount, merchant, token references | per gateway |
| Identity provider | authentication | identity attributes (outside this service) | per provider |
Needs: data-processing agreements, a sub-processor list shared with customers, and a lawful transfer mechanism for any cross-border replication (for example standard contractual clauses). Replicating to AWS in another country is a transfer: choose regions deliberately.

## 9. DPIA summary (to complete)
Processing is systematic evaluation of financial behaviour using automated means, so a DPIA is expected. Main risks and mitigations: wrongful declines (review path, rationale, thresholds tuned on test data); exposure of financial data (encryption, access control); model leaking or mishandling data (feature-only prompts, no training on customer data, prompt-injection controls); excessive logging (masking, retention); cross-border replication (region choice, contracts). Residual risk: [to be assessed with the DPO].

## 10. Breach response (privacy view)
Detect, contain, assess within 24 hours, notify the controller without undue delay. Regulator deadlines to plan for: GDPR 72 hours from awareness (controller); India's CERT-In directions require reporting certain cyber incidents within 6 hours. Confirm current requirements with counsel. Keep a breach register.

## 11. Roles
Data protection officer [name]; privacy lead on the program [name]; security lead; product owner (owns decline messages and notices). Annual review of this document and after any significant change.

## 12. Known gaps (be honest with customers)
1. **Log masking is partial.** It redacts properties with sensitive names and masks card-number-shaped strings. It does not scan free-text messages or exception text. Add a message scrubber and a test.
2. No retention jobs exist yet (outbox clean-up, archive, deletion).
3. No data-subject export or erase tooling yet.
4. The decline-contest and manual review process is not built (FR-13).
5. Region choices and transfer mechanisms are undecided.
6. A DPIA and records of processing need completing and signing.
