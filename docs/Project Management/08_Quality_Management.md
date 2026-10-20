# Quality Management Plan

## 1. Quality objectives (measurable)
| Objective | Metric | Target |
|---|---|---|
| Correct money handling | duplicate financial effects; lost accepted payments in fault tests | 0 and 0 |
| Fast risk path | p95 added latency in Hybrid | at most [300] ms |
| Secure | open high or critical findings at release | 0 |
| Accessible | WCAG 2.2 AA conformance on client | pass |
| Reliable | monthly availability; failover RTO and RPO | [99.9]%; [15] min, [1] min |
| Maintainable | critical code-smell and complexity gates | pass in CI |
| Traceable | requirements with at least one passing test | 100% of Musts |

## 2. Standards
OWASP Top 10 and ASVS-style checks; Clean Architecture dependency rules (Domain depends on nothing; Application only on Domain); WCAG 2.2 AA; semantic versioning; conventional commits.

## 3. Quality assurance (preventing defects)
- **Definition of done:** code reviewed by a second engineer; unit tests; static analysis clean; no new warnings (`TreatWarningsAsErrors` is on); accessibility check for UI; docs updated; requirement ID referenced.
- **Architecture review** at each phase gate; threat model updated when flows change.
- **Pull-request gates in CI:** build, unit tests, integration tests with containers, dependency and secret scanning, SAST, Flutter analyze and test.
- **Agent change control:** prompts and thresholds are versioned code; changing either needs the adversarial test set to pass.

## 4. Quality control (finding defects)
| Level | What | When |
|---|---|---|
| Unit | domain state machine, risk modes, timeout paths, idempotency, log parsing, BLoC logic | every commit |
| Integration | API with real PostgreSQL and Redis; auth, ownership, validation, outbox | every commit |
| Contract | event schemas between producer and consumers; gateway adapters against sandboxes | per release |
| End to end | mobile flows on devices: login, biometric, pay, assistant, logs | nightly and per release |
| Performance | sustained and spike load per NFR-4; p95 and p99 per mode | W18-20, then per release |
| Fault injection | broker down, consumer crash, duplicate delivery, client retry, gateway timeout, model timeout or malformed reply, Redis down, kill between charge and persist | W18-20, W21 |
| Security | SAST, DAST, dependency scan, adversarial prompts, independent pen-test | continuous; pen-test W19-21 |
| Accessibility | automated checks plus screen-reader and large-text manual passes | W17, W23 |
| UAT | pilot-customer scenarios | W23-24 |

## 5. Defect management
Severity 1 (money wrong, data exposure, outage): fix immediately, stop the line. Severity 2 (major function broken, no workaround): fix before the next gate. Severity 3 (minor): scheduled. Severity 4 (cosmetic): backlog. Release rule: zero open Sev 1 and Sev 2; Sev 3 accepted only with owner sign-off. Every defect gets a root cause and, for Sev 1-2, a regression test.

## 6. Metrics reported weekly
Build pass rate; test counts and flake rate; code coverage on Domain and Application (target [80]%); defects found vs fixed by severity; defect escape rate after UAT; mean time to fix; open security findings; SPI and CPI monthly.

## 7. Roles
Product owner accepts deliverables. QA lead owns the test strategy and reports. Engineers own unit and integration tests for their code. Security lead owns security testing and the pen-test relationship. UX lead owns accessibility.

## 8. Audit and reviews
Quality audits at W8, W13, W22: sample requirements traced to tests and evidence. Retrospectives each phase; lessons feed the next phase plan.
