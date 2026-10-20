# Risk Management Plan and Register

## 1. Approach
Risks are identified in workshops at kick-off and each phase gate, and by anyone through the register. Each risk has an owner, a score, a response and a trigger. The program manager reviews the register weekly; the sponsor sees the top five monthly.

**Scoring:** probability (1 rare to 5 almost certain) x impact (1 minor to 5 severe on cost, schedule, quality or reputation). **Score 15 or more = red** (sponsor attention), **8-14 = amber**, below 8 = green. Responses: avoid, mitigate, transfer, accept; and for opportunities, exploit or enhance.

## 2. Register
| ID | Risk | P | I | Score | Response | Owner | Trigger |
|---|---|---|---|---|---|---|---|
| R1 | Microsoft Agent Framework preview API differs from the scaffold or changes during the build | 4 | 4 | 16 | Spike in W3-4; pin versions; agent code isolated behind `IAgentRiskGraph`; fallback to rules-only deep stage | Architect | spike fails by W4 |
| R2 | LLM latency or cost higher than modelled | 3 | 3 | 9 | Hybrid mode limits deep calls; budget alarms; smaller model for behaviour agent | AI engineer | cost per 1,000 payments above target |
| R3 | Prompt injection or unsafe model output affects decisions | 3 | 5 | 15 | feature-only input, strict parsing, fail-toward-review, adversarial test set | Security lead | any bypass in tests |
| R4 | Pen-test finds high or critical issues late | 3 | 4 | 12 | threat model W2-4; SAST/DAST from W5; pen-test starts W19, not later; 3-week fix window | Security lead | high finding open at W22 |
| R5 | Duplicate or lost payments under failure (charge-then-persist window) | 2 | 5 | 10 | gateway-side idempotency keys; reconciliation job; fault-injection tests | Architect | any duplicate in tests |
| R6 | Key person leaves (architect, AI engineer) | 2 | 4 | 8 | pairing, documented decisions, named backup | Program manager | resignation or long absence |
| R7 | Pilot customer unavailable or slips | 3 | 3 | 9 | two candidate pilots; internal pilot fallback | Product owner | no signed pilot by W20 |
| R8 | Compliance scope creep (PCI DSS, local rules) | 3 | 4 | 12 | keep card data with the gateway; legal review W6-10; scope board | Compliance lead | new mandatory control found |
| R9 | Failover between Azure and AWS misses RTO or RPO | 3 | 4 | 12 | early drill in W16, repeat W21; replication-lag monitoring | DevOps | drill misses target |
| R10 | Gateway sandbox access delayed | 3 | 3 | 9 | request in W1; stub gateway keeps team moving | Program manager | no access by W5 |
| R11 | Mobile store review rejects the app | 2 | 4 | 8 | early TestFlight and internal track from W14; follow guidelines for payments | Flutter lead | rejection |
| R12 | Accessibility defects found late | 3 | 3 | 9 | WCAG checks in the definition of done; audit in W17 | UX lead | audit fails |
| R13 | Estimates too optimistic (scaffold never built or load-tested) | 4 | 3 | 12 | week-1 compile and week-4 re-estimate gate; contingency | Program manager | SPI below 0.90 |
| R14 | Revenue assumptions wrong (price, adoption) | 4 | 5 | 20 | validate price and pipeline with buyers before month 3; track pipeline monthly; stage-gate spend | Sponsor | fewer than [3] qualified prospects by month 4 |
| R15 | Dependency vulnerability or supply-chain compromise | 2 | 4 | 8 | pinned versions, SBOM, dependency scanning, release-age policy | DevOps | critical CVE |

## 3. Contingency adequacy (illustrative expected value)
Placeholder probabilities and dollar impacts to test the $63,925 contingency:
| Risk | P | Impact ($) | EMV ($) |
|---|---|---|---|
| R1 agent API rework | 50% | 30,000 | 15,000 |
| R4 pen-test remediation | 40% | 40,000 | 16,000 |
| R6 key-person loss | 20% | 50,000 | 10,000 |
| R2 model cost or latency fixes | 30% | 20,000 | 6,000 |
| R7 pilot delay support | 30% | 15,000 | 4,500 |
| **Total** | | | **51,500** |

Contingency covers this expected value with about $12,000 headroom. A simultaneous R1 + R4 + R6 event ($120,000) would exceed contingency plus reserve ($99,000), so the plan is to descope Should-have items first.

## 4. Escalation and monitoring
Triggers above are checked weekly. Any red risk, any trigger fired, or a forecast to exceed contingency goes to the sponsor within 2 working days. Closed risks are kept for lessons learned. Residual-risk acceptance for security findings is signed by the sponsor.
