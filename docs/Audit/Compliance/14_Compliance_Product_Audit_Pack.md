# Compliance and Product Audit Pack

## 1. Read this first
This pack is the **audit framework**: what an auditor will ask for, where the evidence will come from, and the checklists to run. **No audit evidence exists yet.** The scaffold has not been compiled, its tests have not been run, and no independent testing has happened. Every "Evidence" cell below is therefore **pending** until the release candidate is built and the tests are executed in CI. This is not a certification, and nothing here claims SentinelPay is PCI DSS, SOC 2, ISO 27001 or ADA compliant.

## 2. Scope and objectives
Scope: backend, mobile client, agent engine, infrastructure, processes. Objectives: show that requirements are met and traceable; that financial integrity, security, privacy and accessibility controls operate; that AI decisions are governed and auditable; and that gaps are known and owned.

## 3. Evidence repository
Keep evidence in a versioned, access-controlled store, one folder per release: `/requirements`, `/design` (ADRs, threat model), `/code-review` (PR records), `/ci` (build, test, scan reports), `/performance`, `/fault-injection`, `/security` (SAST, DAST, pen-test, closure), `/privacy` (DPIA, records of processing), `/accessibility`, `/operations` (runbooks, drills), `/ai-governance`, `/vendors`, `/sign-offs`. Each item gets an ID, date, author and the commit or build it relates to. Evidence is never edited after filing; corrections are new versions.

## 4. Requirements traceability matrix
> **Update:** after the matrix below was drafted, tests were written for most of the listed gaps (see `docs/TEST_FINDINGS.md` in the scaffold): consumer handler, assistant scoping, agent fallback and parsing, outbox relay, audit trigger, security headers, rate limits, JWT checks, duplicate-request race, PII masking, client accessibility, plus a k6 load script and a fault-injection runbook. They are **written, not run**. Still without tests: provider switching (FR-10), failover, and the not-built requirements. The Gap column below describes the state before those tests.

Status: **Built** = code exists in the scaffold; **Not built** = planned only. "Test written" = a test exists in the scaffold; all are **not yet run**.

| Req | Requirement | Code | Test written | Gap before audit |
|---|---|---|---|---|
| FR-1 | Idempotent payment creation | `CreatePaymentHandler`, unique index | `Duplicate_key_returns_original_without_second_row` | concurrency race test with real Redis |
| FR-2 | Ownership check | handlers, payment read | `Other_users_account_is_not_found`, `Another_users_account_returns_404` | none |
| FR-3 | Fast check with timeout policy | `TryScoreFastAsync`, `RiskOptions` | `Sync_timeout_follows_configured_action`, `Sync_timeout_can_fail_closed` | none |
| FR-4 | Hybrid decision | `CreatePaymentHandler` | `Hybrid_high_score_declines_immediately`, `..._low_score_skips_deep_stage`, `..._mid_score_defers_to_deep`, `..._large_amount_always_goes_deep` | none |
| FR-5 | Atomic outbox and relay | `CreatePaymentHandler`, `OutboxRelay` | `Accepted_payment_writes_transaction_and_outbox_together` | **no test for the relay**, broker-down test |
| FR-6 | Deep risk by agent graph | `RiskGraph` | none | **parser, graph and fallback tests**; adversarial prompt set |
| FR-7 | Routing, charge, settle | `PaymentProcessingHandler` | state transitions in `TransactionTests` | **no handler tests**: redelivery, gateway failure, insufficient funds |
| FR-8 | Append-only audit | `AgentAuditLog`, `audit_immutable.sql` | timeout audit row asserted | **trigger test**, completeness test (every decision has a row) |
| FR-9 | Natural-language search, scoped | `AssistantQueryService`, `QuerySpecParser` | none | **scope, clamp and parser-injection tests** |
| FR-10 | Broker selectable | `MessagingOptions`, publishers | none | integration test per provider |
| FR-11 | Biometric confirmation | `BiometricGate`, `PaymentBloc` | `does not call the server when biometrics fail` | device test |
| FR-12 | Log viewer | `LogQueryService`, logs screens | `LogQueryServiceTests`, `LogsCubit` test, `Log_viewer_is_ops_only` | log-rotation test |
| FR-13 | Manual review queue | not built | | **build before go-live** (see doc 11) |
| FR-14 | Refunds | not built | | planned 1.1 |
| FR-15 | Admin console | not built | | planned 1.1 |
| NFR-1 | Risk latency | | none | **performance test** |
| NFR-3 | Failover RTO/RPO | IaC not built | none | **drills W16, W21** |
| NFR-5 | Integrity under faults | outbox, idempotency | partial | **fault-injection suite** |
| NFR-6 | Security | headers, auth, validation | `Invalid_input_is_rejected_before_any_work` | header, rate-limit, pen-test evidence |
| NFR-7 | Privacy | masking enricher | none | **masking test, free-text gap** |
| NFR-8 | Accessibility | semantic labels, live regions | none | **widget accessibility tests, assistive-tech audit** |
| NFR-9 | Correlation IDs | middleware, client header | none | end-to-end trace test |

**Test gaps to close (priority order):** PaymentProcessingHandler; fault injection and load; PII masking; AssistantQueryService scope; OutboxRelay; agent parser and adversarial set; security headers and rate limits; accessibility widget tests; audit trigger.

## 5. Control mappings
### 5.1 OWASP Top 10 (2021)
Use the control map in document 13 section 3. Evidence per category: A01 authorisation tests and pen-test results; A02 TLS and key configuration scans; A03 injection tests and DAST; A04 threat model and design review; A05 header and configuration scan; A06 dependency scan and SBOM; A07 identity-provider configuration and test results; A08 build-signing and branch-protection settings; A09 log samples, alert tests; A10 code review of outbound calls.

### 5.2 PCI DSS v4.0 (scope reduction view)
The platform does not store, process or transmit card numbers when card entry is handled by the gateway's hosted fields or SDK. Which self-assessment questionnaire applies depends on the integration, so **confirm with a Qualified Security Assessor**. Control families to evidence either way:
| Family | Evidence |
|---|---|
| Network security controls | Front Door and Application Gateway configuration, network rules |
| Secure configurations | hardening baselines, IaC reviews |
| Protect stored account data | proof that no PAN is stored: data inventory, database scan, log scan |
| Protect data in transit | TLS configuration scan, pinning |
| Malware and vulnerabilities | patching records, dependency scans |
| Secure development | SDLC, reviews, SAST/DAST |
| Access control and identification | roles, MFA, access reviews |
| Logging and monitoring | log retention, alert tests, time sync |
| Testing | pen-test, vulnerability scans |
| Policies and risk | security policy, risk register, awareness training |

### 5.3 GDPR and DPDP Act (design view)
| Article or topic | Evidence |
|---|---|
| Art. 5 principles | data inventory, retention schedule (doc 12) |
| Art. 6 lawful basis | basis and balancing-test record per customer |
| Art. 22 automated decisions | review path, stored rationale, contest procedure |
| Art. 25 by design | feature-only agent input, minimisation, design review |
| Art. 28 processors | data-processing agreements, sub-processor list |
| Art. 30 records of processing | register |
| Art. 32 security | doc 13 controls and test evidence |
| Art. 33-34 breach | incident plan, breach register, tabletop record |
| Art. 35 DPIA | signed DPIA |
| DPDP Act obligations | counsel's mapping to the customer's role |

### 5.4 Accessibility (ADA and WCAG)
The ADA does not set a technical standard for mobile apps in the text of the statute. **WCAG 2.1 or 2.2 level AA is the widely used benchmark**, and US rules for some public bodies reference WCAG 2.1 AA. Confirm the standard your customers expect with counsel. India's Rights of Persons with Disabilities Act 2016 may also apply to your customers.

| WCAG 2.2 criterion | Implementation in the client | Status |
|---|---|---|
| 1.1.1 Non-text content | icons carry semantic labels (log status icons) | built, to verify |
| 1.3.1 Info and relationships | Material widgets and `Semantics` | built, to verify with a screen reader |
| 1.4.1 Use of colour | log status uses icon, label and text | built |
| 1.4.3 Contrast | theme from a seed colour in light and dark | **to measure** |
| 1.4.4 Resize text | system text scaling honoured | to verify at 200% |
| 2.1.1 Keyboard / switch access | standard controls | to verify |
| 2.4.x Navigation | titled screens | to verify |
| 2.5.8 Target size (minimum) | 48 dp primary button, default icon buttons | built, to verify |
| 3.3.1 Error identification | errors shown as text in a live region | built |
| 3.3.2 Labels or instructions | every field has a label | built |
| 4.1.3 Status messages | live regions for payment result and log errors | built |
Evidence: automated checks, manual screen-reader pass on iOS (VoiceOver) and Android (TalkBack), large-text pass, contrast measurements, a written conformance statement (VPAT-style).

## 6. AI governance evidence
| Topic | Evidence to hold |
|---|---|
| Model and prompt inventory | model ID, provider, region, prompt versions, who approved |
| Change control | any prompt or threshold change ticketed, tested against the adversarial set, approved |
| Human oversight | review band, review queue, contest process, sampling of declines |
| Decision record | audit rows: score, rationale, model, latency, input hash, timeout flag |
| Robustness | prompt-injection tests, malformed-reply tests, fallback-to-rules tests |
| Performance monitoring | decline and review rates, false-decline sample review, drift check, threshold review [quarterly] |
| Data handling | no personal data in prompts; provider no-training terms |
Regulatory note: the EU AI Act's high-risk list covers some credit-scoring uses and, as I recall, excludes systems used to detect financial fraud. Confirm classification with counsel for each market.

## 7. Audit procedure
1. **Plan:** scope, criteria, auditors independent of the build team, dates (internal audits at weeks 8, 13, 22; pre-go-live readiness audit week 22-23; external after go-live as required by customers).
2. **Collect:** pull evidence from the repository; re-run sampled tests; inspect configuration directly.
3. **Sample:** 100% of Sev 1-2 defects and security findings; at least 25 randomly chosen transactions traced end to end from API to audit to log; all changes to prompts, thresholds and permissions in the period.
4. **Conclude:** classify each item **Conforms**, **Minor nonconformity**, **Major nonconformity** or **Observation**.
5. **Correct:** owner, action, due date (major: 14 days; minor: 45 days); verify closure; track in the findings log.
6. **Report and sign:** audit report to the steering committee; release decision records open majors (none allowed at go-live).

## 8. Audit checklist
Mark each: Y / N / N/A, with evidence ID.
**Financial integrity**
1. Repeating a request with the same key returns the original result and creates no second charge.
2. A payment cannot be routed unless approved (state machine test).
3. Every accepted payment has a matching outbox event; none is lost when the broker is down.
4. Redelivered events create no second effect.
5. Reconciliation with gateway records shows no unmatched items.
**Access and security**
6. Users can only read and create payments on their own accounts.
7. Operations endpoints reject non-ops users.
8. Token validation checks issuer, audience and expiry.
9. Rate limits trigger and return 429.
10. Security headers present on all responses; error responses reveal no stack traces.
11. No secrets in source or configuration; secrets come from the vault.
12. SAST, DAST and dependency scans show no open critical or high items.
13. Pen-test report exists; findings closed and retested.
**Data protection**
14. Data inventory matches the database schema; no names or card numbers stored.
15. Logs contain no card-number-shaped strings or direct identifiers in a sampled week.
16. Retention jobs run and delete as scheduled.
17. Data-subject request handled in a test within the legal period.
18. Processor agreements and sub-processor list current.
**Logging and audit**
19. Every agent decision has an audit row; update and delete on the table are blocked.
20. A sampled correlation ID can be traced through client, API, event, audit row and logs.
21. Log files rotate and are retained as specified; viewer access limited to ops.
**Change and release**
22. All changes reviewed; protected branches; build artefacts signed.
23. Prompt and threshold changes follow the approval and test process.
24. Release has gate sign-offs M1-M6.
**Resilience**
25. Failover drills met RTO and RPO; evidence filed.
26. Backup restore tested.
27. Incident plan exercised in a tabletop.
**AI governance**
28. Fail-toward-review verified for malformed model replies.
29. Review path and contest process operate.
30. Decline and review rates monitored with a documented response.
**Accessibility**
31. Conformance statement published; screen-reader and large-text passes recorded.
32. Accessibility defects triaged under the same severity scheme.
**Vendors**
33. Vendor risk reviews done; contracts include security and data terms.

## 9. Templates
**Findings log:** ID | date | area | requirement or control | finding | classification | owner | action | due | status | closure evidence.
**Evidence index:** evidence ID | title | requirement IDs | source (build, commit, tool) | date | author | location | retention.
**Sign-off:** release version | audit period | auditors | scope | counts (conforms, minor, major, observations) | open items accepted by sponsor with expiry | signatures and dates.

## 10. Readiness status (today)
Requirements built in scaffold: 12 of 15. Requirements with at least a partial test written: 11 of 15 (FR-1 to 9, 11, 12), up from 8 after the follow-up test work. Tests executed: 0. Independent testing: none. Open known gaps: listed in section 4 and in docs 12 and 13. The path to audit readiness runs through weeks 18-22 of the plan.
