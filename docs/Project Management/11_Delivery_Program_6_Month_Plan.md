# Six-Month Delivery Program Plan

Companion to documents 03 (charter), 05 (schedule), 06 (cost) and 07 (risk). Assumed start **2 November 2026**, go-live **30 April 2027**. Dates move with the real start.

## 1. Program governance
| Body | Members | Cadence | Decides |
|---|---|---|---|
| Steering committee | sponsor, product owner, program manager | monthly and at gates | funding tranches, gate passage, reserve use, scope over 1 week or $10,000 |
| Program board | program manager, architect, QA, security, DevOps, UX leads | weekly | priorities, risks, dependencies, release readiness |
| Sprint team | all engineers | two-week sprints (13 sprints) | how to build; backlog order within agreed priorities |
| Architecture forum | architect, tech leads, security lead | weekly, 45 min | design changes, ADRs, threat-model updates |
| Change board | program manager, product owner, architect | as needed | change requests under steering thresholds |

**Ways of working:** Scrum sprints inside stage gates; trunk-based development with short-lived branches; every change reviewed; CI gates from week 2; architecture decisions recorded as short ADRs; demos every sprint.

## 2. Streams
1. **Platform:** backend, outbox, brokers, idempotency, gateway adapters, identity.
2. **Risk and agents:** fast scorer, mode logic, agent graph, query agent, audit.
3. **Client:** Flutter flows, security, assistant, log viewer, accessibility.
4. **Infrastructure:** CI/CD, Azure edge and hosting, AWS failover, observability.
5. **Quality and security:** test strategy, performance, fault injection, pen-test, compliance evidence.
6. **Readiness:** documentation, training, pilot, go-live.

## 3. Staffing by month (FTE, from the cost model)
| Month | 1 | 2 | 3 | 4 | 5 | 6 |
|---|---|---|---|---|---|---|
| Total FTE | 8.0 | 11.0 | 12.0 | 12.5 | 11.0 | 8.75 |
| Labor cost ($) | 72,000 | 94,000 | 101,500 | 105,000 | 91,500 | 72,250 |

## 4. Week-by-week plan
| Wk | Dates (from) | Focus | Key outputs |
|---|---|---|---|
| 1 | 2 Nov | Mobilise | team onboarded; scaffold compiled; backlog; environments requested; gateway sandbox requested |
| 2 | 9 Nov | Pipelines | CI/CD live; cloud subscriptions; threat model begins; ADR log started |
| 3 | 16 Nov | Agent spike | Agent Framework spike on restored version; IaC baseline; identity tenant requested |
| 4 | 23 Nov | **Gate M1** | build green; spike decision (use framework or rules-only fallback); re-estimate |
| 5 | 30 Nov | Payment core | payment flow hardened; migrations; gateway sandbox connected |
| 6 | 7 Dec | Messaging and identity | outbox relay on both brokers; identity provider integration; legal and privacy review starts |
| 7 | 14 Dec | Reliability | consumer idempotency proven; gateway adapter 1; load smoke test |
| 8 | 21 Dec | **Gate M2** (light week, holidays) | end-to-end payment with one gateway; integration tests green |
| 9 | 28 Dec | Risk logic | fast scorer tuned; all four modes and timeout policies; agent graph build starts |
| 10 | 4 Jan | Agents and audit | agent graph and audit store; pen-test vendor booked; legal review ends |
| 11 | 11 Jan | Query agent | natural-language search; adversarial prompt test set |
| 12 | 18 Jan | Mode matrix | mode and timeout matrix tests; pilot discovery interviews |
| 13 | 25 Jan | **Gate M3** | all modes working; decisions audited; fail-toward-review verified |
| 14 | 1 Feb | Client core | payment flow, pinning, biometrics; first test builds to testers |
| 15 | 8 Feb | Assistant and ops | assistant screen; log API polish; accessibility checks in done-definition |
| 16 | 15 Feb | Log viewer and failover | log viewer; **failover drill 1** |
| 17 | 22 Feb | **Gate M4** | client feature-complete; accessibility audit; store submission dry run |
| 18 | 1 Mar | Performance | load tests per mode; fault injection starts |
| 19 | 8 Mar | Pen-test starts | pen-test week 1; fault-injection catalogue complete |
| 20 | 15 Mar | Fix and confirm | findings triaged and fixed; pilot confirmed; production gateway onboarding |
| 21 | 22 Mar | Remediation | pen-test ends; remediation; **failover drill 2** |
| 22 | 29 Mar | **Gate M5** and security freeze | no open high or critical findings; evidence pack complete; release branch cut |
| 23 | 5 Apr | UAT | UAT with pilot customer; documentation finalised |
| 24 | 12 Apr | Release candidate | UAT sign-off; RC built; **one-week buffer held** |
| 25 | 19 Apr | Pilot live | pilot on production under watch; admin training |
| 26 | 26 Apr | **Gate M6 go-live** | go-live decision; handover to sustain team; hypercare begins |

## 5. Stage gates
| Gate | Week | Exit criteria |
|---|---|---|
| M1 | 4 | CI green; environments up; agent decision made; estimates reviewed; threat model v1 |
| M2 | 8 | FR-1, 2, 5, 7 pass; outbox and idempotency tests; no Sev 1 open |
| M3 | 13 | FR-3, 4, 6, 8, 9, 10 pass; mode matrix and timeout tests; adversarial set passes |
| M4 | 17 | FR-11, 12 pass; WCAG audit has no blockers; failover drill 1 evidence |
| M5 | 22 | NFR-1 to 5 met in tests; zero open Sev 1-2; pen-test high and critical closed; evidence pack complete |
| M6 | 26 | UAT signed; runbooks and training done; rollback rehearsed; sponsor approves |
Each gate is a one-hour review with a checklist; a failed gate means a recovery plan within 2 working days, not a silent slip.

## 6. Integration and environment plan
Environments: dev (per developer, Docker Compose), test (shared, auto-deployed from main), staging (production-like on Azure with the AWS standby), production. Promotion by pipeline only; infrastructure as code; configuration (risk mode, thresholds, gateways) managed per environment, with changes reviewed. Data in non-production is synthetic.

## 7. Release and go-live plan
- **Risk-mode rollout:** pilot starts in Hybrid with conservative thresholds chosen so the number of payments entering Review stays small enough for the review team to handle and `OnSyncTimeout = DeferToDeep`. Change the mode only through reviewed configuration.
- **Go-live dependency (important):** the Review band holds payments for a person to decide, but the manual review queue (FR-13) is currently a Should-have and **not built**. A minimum viable review path (list held payments, approve or decline, audit entry) must be added to the plan before go-live, or Review outcomes must be mapped to decline for the pilot. Decide by week 12.
- **Cutover:** freeze at M5; release candidate in week 24; production deploy in a maintenance window; smoke tests; pilot traffic limited by a per-customer cap in week 25; full go-live week 26.
- **Rollback:** previous container image and database migration rollback rehearsed in staging; feature flags for the assistant and deep stage; brokers drained before rollback.
- **Hypercare:** 30 days after go-live; daily health review; Sev 1 on-call; weekly summary to sponsor; exit when no Sev 1-2 in 14 days and SLOs met.

## 8. Program KPIs
Schedule: SPI at or above 0.95. Cost: CPI at or above 0.95 and EAC within BAC plus contingency. Quality: zero open Sev 1-2 at gates; Musts with passing tests 100% at M6. Delivery: sprint commitments met at least 80%. Risk: red risks at most 2, each with an owner and dated action. Pilot: customer satisfaction at least [4 of 5]; no payment-integrity incident.

## 9. Reporting calendar
Weekly one-page status; monthly cost and EVM review (workbook); gate reports at M1-M6; fortnightly demo; final close-out report with lessons learned in week 26.

## 10. Close-out
Handover to the sustain team (runbooks, dashboards, on-call rota); final cost reconciliation against BAC; release of unused contingency and reserve; lessons learned workshop; archive of decisions, test and security evidence.
