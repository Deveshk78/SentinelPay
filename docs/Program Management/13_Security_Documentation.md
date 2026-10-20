# Security Documentation

**Status:** design and control description for the scaffold, with an honest status per control. It is not a certification and no independent testing has happened yet.
Status key: **Done** = in the scaffold code; **Partial**; **To do**.

## 1. Security architecture in brief
Mobile app -> Azure Front Door and Application Gateway (WAF, TLS) -> API (.NET 9) -> PostgreSQL, Redis, broker; agent engine behind an interface calling Azure OpenAI by managed identity; AWS warm standby. Trust boundaries: device to edge; edge to API; API to data stores; API to model provider; Azure to AWS replication; operator access to logs and audit.

## 2. Threat model (STRIDE, main items)
| Element | Threat | Control | Status |
|---|---|---|---|
| Mobile client | spoofed server, interception | TLS with certificate pinning (current plus backup pin) | Done (pins are placeholders) |
| Mobile client | token theft | Keychain/Keystore storage; short-lived tokens | Done / token lifetime policy To do |
| Mobile client | rooted or tampered device | jailbreak and root detection, app attestation, obfuscation | To do |
| Mobile client | shoulder-surfing, screenshots | hide sensitive screens in app switcher; block screenshots on payment screen | To do |
| API | spoofing identity | JWT validation: issuer, audience, lifetime, 30 s skew | Done (replace symmetric key with identity provider) |
| API | tampering, replay | idempotency key; unique index on (account, key); Redis lock | Done |
| API | information disclosure, BOLA | owner check on every account and transaction read; query scope forced server-side | Done |
| API | denial of service | rate limits (120 per minute, assistant 20), request size limits, WAF | Partial (size limits and WAF rules To do) |
| API | elevation of privilege | role policy "Ops" on log and audit endpoints | Done |
| Event bus | message forgery or tampering | private network, broker authentication, TLS, managed identity for Service Bus; consumers validate type and shape | Partial (message signing To do) |
| Event bus | poison messages | retry once then dead-letter; alert on dead-letter depth | Done / alert To do |
| Database | SQL injection | EF Core parameterised queries; no raw SQL except a fixed outbox query | Done |
| Database | audit tampering | trigger blocks update and delete on agent_audit_logs; restricted DB roles | Done (script must be applied) |
| Agents | prompt injection | feature-only input in a delimited data block, merchant ID whitelist, system prompt marks input untrusted | Done (reduces, not eliminates) |
| Agents | unsafe output | strict JSON parse; clamp score 0-100; unparseable becomes score 60 (review) | Done |
| Agents | excessive agency | agents have no tools and no write access; thresholds and state changes are code | Done |
| Query agent | data exfiltration | model emits a whitelisted filter only; server enforces account scope, limit and parameterisation | Done |
| Model provider | data leakage, cost abuse | no personal data in prompts; managed identity; rate limit; spend alarms | Done / alarms To do |
| Logs | PII in logs, log tampering | masking enricher; ops-only access; central immutable store | Partial |
| Failover | stale or exposed replica | encrypted replication, separate credentials, same controls in AWS | To do |

## 3. OWASP Top 10 (2021) control map
| Category | Controls and where | Status |
|---|---|---|
| A01 Broken access control | owner checks in `CreatePaymentHandler`, `AssistantQueryService`, payment read; "Ops" policy | Done |
| A02 Cryptographic failures | TLS everywhere; HSTS; pinning; Key Vault for secrets; database and backup encryption at rest | Partial (at-rest and Key Vault configured in infrastructure code later) |
| A03 Injection | EF parameterised queries; input validation (amount range, currency, merchant pattern, key length); whitelisted agent output | Done |
| A04 Insecure design | outbox, idempotency, state machine, fail-closed timeout option, rate limits, threat model | Done |
| A05 Security misconfiguration | security headers (nosniff, DENY frames, no-referrer, CSP `default-src 'none'`, no-store, Permissions-Policy); errors return a generic problem response with a correlation ID | Done |
| A06 Vulnerable components | central package management, dependency scanning and SBOM in CI, pinned versions | To do (pipeline) |
| A07 Identification and authentication failures | JWT checks; biometric gate per payment; MFA, refresh tokens, lockout via the identity provider | Partial |
| A08 Software and data integrity failures | signed builds, protected branches, container image signing, review of CI secrets | To do |
| A09 Logging and monitoring failures | structured rotating logs, correlation IDs, request logging, in-app viewer, audit table | Done / alerting To do |
| A10 SSRF | no user-supplied URLs are fetched; outbound calls only to configured endpoints | Done |

## 4. OWASP Mobile (MASVS) and LLM Top 10 mapping
**Mobile:** storage (Keychain/Keystore) Done; network (pinning, TLS) Done; authentication (biometric with passcode fallback, fail closed when no secure lock) Done; platform and code quality (analyzer, strict casts) Partial; resilience (root detection, obfuscation, attestation) To do.
**LLM risks:** prompt injection Done (reduced); insecure output handling Done (strict parsing); excessive agency Done; sensitive information disclosure Done (features only); model denial of service Partial (rate limit, timeout, fallback to rules); supply-chain (model and framework versions) pinned; overreliance mitigated by review band and audit.

## 5. Identity and access management
End users and operators authenticate through the identity provider (to be connected; the scaffold's symmetric key is a stand-in). Roles: customer user, ops, security auditor, administrator. Privileged access: least privilege, just-in-time elevation, MFA, and a quarterly access review. Service identities use managed identities; no shared secrets in configuration.

## 6. Data protection and key management
TLS 1.2 or higher. Encryption at rest for database, backups and storage using provider-managed keys, with customer-managed keys where the customer requires it. Secrets in Key Vault; rotation every [90 days] or on staff change. Field-level encryption for any sensitive field added later. Tokenise or avoid card data: the platform never handles card numbers.

## 7. Secure development lifecycle
Threat model updated at each gate; peer review on every change; SAST and dependency scans on every pull request; secret scanning; DAST against staging; container and IaC scanning; signed artefacts; adversarial prompt tests for any prompt or threshold change; security sign-off at M5 and before go-live.

## 8. Vulnerability management
| Severity | Fix target |
|---|---|
| Critical (exploitable, money or data at risk) | 48 hours, release stopped |
| High | 7 days |
| Medium | 30 days |
| Low | next release |
Exceptions need the security lead's written reason and the sponsor's acceptance with an expiry date.

## 9. Monitoring and alerting (to build)
Alert on: authentication failures spike; 403 on ops endpoints; rate-limit rejections; dead-letter depth; outbox age above [60 s]; agent timeouts and parse failures; model spend; unusual decline or review rates; failover events; audit-table trigger violations; error-rate and latency SLOs. Route to the on-call rota.

## 10. Incident response
| Phase | Actions | Owner |
|---|---|---|
| Detect and triage | alert or report; assign severity (Sev 1 money or data at risk, Sev 2 degraded security control) | on-call |
| Contain | revoke tokens or keys; switch risk mode to a stricter setting; disable the assistant or deep stage by flag; block abusive clients | incident lead |
| Eradicate and recover | patch, rotate secrets, restore from backup or fail over, verify integrity by reconciliation | engineering lead |
| Communicate | sponsor within 1 hour for Sev 1; customer and regulator notices per contract and law | program manager, privacy lead |
| Learn | blameless review within 5 working days; actions tracked | security lead |
Run a tabletop exercise before go-live and annually.

## 11. Resilience and disaster recovery
Targets (to confirm): RTO [15] min, RPO [1] min. Azure primary; AWS warm standby with database replication; traffic shifted by health probes at the edge. Drills in weeks 16 and 21. Backups tested by restore. Reconciliation job compares gateway records with ledger after any failover (to build).

## 12. Penetration test scope (week 19-21)
Mobile app (static and dynamic), API (authentication, authorisation, injection, business-logic abuse such as replay and race conditions), event bus configuration, cloud configuration review for Azure and AWS, agent prompt-injection and data-exfiltration attempts, log endpoints. Report with proof of exploit and retest of fixes.

## 13. Known gaps in the current scaffold
1. Symmetric JWT signing key; no refresh tokens, revocation or MFA.
2. Pinned certificate hashes are placeholders; the pin is a whole-certificate hash, which breaks on rotation unless a backup pin is current.
3. No root detection, attestation, screenshot blocking or obfuscation in the client.
4. HSTS and the development database creation (`EnsureCreated`) are environment-dependent; production must use migrations and verified TLS settings.
5. No request-size limits, WAF rules, or alerting yet.
6. The charge-then-persist crash window needs gateway-side idempotency and reconciliation.
7. Agent Framework integration is unverified against a restored package version.
8. Log masking is partial (see privacy document).
9. No message signing between producer and consumers.
