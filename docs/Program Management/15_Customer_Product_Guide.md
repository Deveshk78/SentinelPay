# SentinelPay Product Guide

*Customer-facing draft. Written ahead of release from the current design; items marked **Planned** are not in version 1.0. Verify every statement against the released build before distributing.*

## 1. What SentinelPay is
SentinelPay is a payment platform you host for your customers: a mobile app, an API, and a risk engine that checks payments with a mix of fast rules and AI agents. Its distinctive feature is that **you choose where risk checking happens**: before a payment is accepted, after, or a hybrid, with one setting.

## 2. Why customers choose it
- **Control over speed and safety.** Decide how much checking happens while the customer waits.
- **No lost or doubled payments by design.** Each accepted payment is recorded together with its event, and repeat requests never charge twice.
- **Explainable AI.** Every agent decision is stored with a score and a reason, in a table that cannot be edited.
- **Less personal data.** The platform stores no names or card numbers.
- **Built for operations.** An in-app log viewer shows normal runs and errors, and traces any single payment end to end.
- **Accessible.** The mobile app is built for screen readers, large text and clear status messages.

## 3. How it works
1. The customer confirms a payment with their fingerprint, face or device passcode.
2. The app sends it securely, with a unique key that prevents double charging.
3. The platform runs a quick check within a time limit you set.
4. It saves the payment and its event in one step, and replies immediately.
5. AI agents analyse the payment more deeply where needed, before any money moves.
6. The payment goes to the best available payment gateway and settles.

## 4. Risk modes
| Mode | What it does | Choose it when |
|---|---|---|
| Off | no risk scoring | testing only |
| Sync | quick check decides immediately | you want a decision before accepting every payment |
| Async | accept first; AI agents decide before money moves | you need the fastest reply |
| **Hybrid** (recommended) | quick check first: clear declines stop at once, low risk goes straight through, the rest goes to the agents | most deployments |
If the quick check is too slow you choose what happens: **defer to the agents** (default), **decline**, or **approve**. Payments can also be **held for review** when the score is in the middle band.

## 5. Features in version 1.0
Payments with biometric confirmation; idempotent API; four risk modes; AI risk analysis with audit trail; gateway routing by policy; plain-language transaction search ("show declined payments over 5,000 last week"), limited to the user's own account; operations log viewer with error summary and request tracing; RabbitMQ or Azure Service Bus messaging; Azure hosting with AWS standby.
**Planned (not in 1.0 unless confirmed):** manual review console, refunds and reversals, customer admin console, customer event notifications (webhooks), data export and erase tooling.

## 6. Security and privacy summary
Designed around the OWASP Top 10: encrypted connections with certificate pinning in the app, secure token storage, per-account access checks, rate limits, security headers, and strict handling of AI output. AI agents see only numbers and a sanitised merchant ID, never names or card numbers, and agents cannot move money or read data directly. The platform keeps no card numbers (your payment gateway does). Documents available under NDA: security overview, privacy summary, audit pack, penetration-test summary. SentinelPay is **designed to support** your compliance work; certifications are listed in the compliance summary only once awarded.

## 7. Deployment
Hosted on Microsoft Azure behind Front Door and Application Gateway, with a warm standby on AWS for failover. Data stores: PostgreSQL and Redis. Region choices and data-transfer terms are agreed per contract. Recovery targets (planned): recovery time [15] minutes, data loss window [1] minute.

## 8. API quick reference
Base path `/api`. All calls need `Authorization: Bearer <token>`. Replies are JSON. Every reply carries `X-Correlation-Id`; quote it to support.

### Create a payment
`POST /api/payments`
Headers: `Idempotency-Key` (8-100 characters, required), `X-Correlation-Id` (optional, 8-64 letters, digits or hyphens).
```json
{ "accountId": "7d1b...", "amount": 250.00, "currency": "INR", "merchantId": "shop-1" }
```
Rules: amount 0.01 to 10,000,000; currency three uppercase letters; merchantId letters, digits, `-` or `_`, up to 64 characters.
Reply `202 Accepted`:
```json
{ "transactionId": "c3f1...", "status": "PendingRisk", "riskScore": null, "message": "Payment accepted; risk checks in progress." }
```
| Code | Meaning |
|---|---|
| 202 | accepted (status may be Approved, PendingRisk or RiskReview) |
| 400 | validation failed; details in the reply |
| 401 | missing or invalid token |
| 404 | account not found or not yours |
| 409 | a request with this key is already in progress |
| 422 | declined or rejected (message explains) |
| 429 | too many requests; wait and retry |
| 500 | server error; reply includes a correlation ID |
**Retry rule:** on a network failure, retry with the **same** Idempotency-Key. A new key means a new payment.

### Read a payment
`GET /api/payments/{id}` returns id, amount, currency, merchantId, status, createdAt. Statuses: `PendingRisk`, `RiskReview`, `Approved`, `Declined`, `Routed`, `Settled`, `Failed`. Poll this endpoint until the status is final. **Planned:** event notifications.

### Ask about transactions
`POST /api/assistant/query` with `{ "accountId": "...", "question": "declined payments over 5000 last week" }` (question up to 500 characters). Returns a summary and matching rows. Limited to 20 requests per minute per user.

### Operations (ops role only)
`GET /api/ops/logs?level=&correlationId=&q=&from=&to=&take=` (up to 500 lines); `GET /api/ops/logs/summary?hours=24`; `GET /api/ops/audit/{transactionId}`; `GET /health` (public).

## 9. Configuration reference
Settings live in configuration and can be changed without new code; changes should be reviewed and recorded.
| Setting | Default | Meaning |
|---|---|---|
| `Risk:Mode` | Hybrid | Off, Sync, Async, Hybrid |
| `Risk:SyncTimeoutMs` | 400 | time limit for the quick check |
| `Risk:OnSyncTimeout` | DeferToDeep | DeferToDeep, Decline or Approve |
| `Risk:ReviewThreshold` / `DeclineThreshold` | 50 / 80 | score bands for review and decline |
| `Risk:SkipDeepBelowScore` | 20 | low scores skip the agent stage (Hybrid) |
| `Risk:AlwaysDeepAbove` | 50,000 | larger amounts always go to the agents |
| `Risk:SyncUsesAgent` | false | use agents in the quick check (slower) |
| `Messaging:Provider` | RabbitMq | RabbitMq or ServiceBus |
| `Gateways:Providers` | Sandbox | list of gateways with fee in basis points and on/off |
| `Agents:Enabled` | false | turn on AI analysis and search |
| `Logging:Directory` | logs | where rotating logs are written |
Tuning advice: raise the decline threshold only after reviewing a sample of declined payments; lower `SyncTimeoutMs` if checkout feels slow, and watch how many payments time out.

## 10. The mobile app
Pay with biometric confirmation; clear error messages; works with screen readers and large text; status shown with icons and words, not colour alone. Operations users also get **Run logs**: a summary of errors in the last 24 hours, filters by level and text, and a tap-to-trace view showing every log line for one request.

## 11. Operations guide
**Daily:** open Run logs and check the summary; look at the review and decline rates; check the message backlog. **When something fails:** copy the correlation ID from the app or API reply, open Run logs, tap Trace, then fetch the audit rows for that transaction. **Alerts to set** (see security guide): authentication spikes, dead-letter depth, outbox age, timeout rate, model spend. **Changing risk settings:** use the change process; record who, why and the result.

## 12. Support and service levels (placeholders)
| Severity | Example | Response | Update |
|---|---|---|---|
| 1 | payments wrong or service down | [30 minutes], 24x7 | hourly |
| 2 | major function degraded | [4 hours] | daily |
| 3 | minor issue | [1 business day] | weekly |
Support contact: [email, phone]. Availability target: [99.9]% monthly, defined in your contract.

## 13. Limitations
Version 1.0 does not include refunds, a review console, or webhooks (see section 5). AI analysis reduces risk and does not remove it; false declines and missed fraud are possible, which is why the review band, audit trail and threshold tuning exist. Risk results depend on your configuration and data.

## 14. FAQ
**Does SentinelPay store card numbers?** No. The payment gateway does.
**Can the AI move money?** No. Agents produce a score and reason; fixed rules and code decide what happens next.
**What if the AI service is down?** The platform falls back to its rule-based scorer, and unreadable AI replies are sent to review.
**Can we see why a payment was declined?** Yes, from the audit record: score, reason and timing.
**Can we switch from Async to Hybrid later?** Yes, by configuration.

## 15. Glossary
**Idempotency key:** a unique code that makes repeated requests safe. **Outbox:** a record saved with the payment so no event is lost. **Agent:** an AI component that analyses one aspect of risk. **Correlation ID:** one identifier that follows a request everywhere. **Review band:** scores that hold a payment for a person.

## 16. Release notes (template)
Version [1.0.0], date [ ], new features, fixes, known issues, upgrade notes, configuration changes, security updates.
