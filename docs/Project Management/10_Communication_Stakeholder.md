# Communication and Stakeholder Engagement Plan

## 1. Stakeholder register
| Stakeholder | Interest | Influence | Needs | Strategy |
|---|---|---|---|---|
| Sponsor | return on investment, risk | high | monthly cost, schedule, risk, decisions | manage closely |
| Product owner | scope, quality, priorities | high | weekly progress, demos | manage closely |
| Engineering team | clear work, tools | medium | priorities, quick decisions | involve daily |
| Security and compliance lead | control evidence | high | design and test evidence | manage closely |
| Pilot customer(s) | working product, low disruption | medium-high | demos, UAT access, a named contact | engage from W12 |
| Prospective customers | proof, references | medium | roadmap, compliance pack, demos | keep informed; feedback on price |
| Payment gateway and cloud vendors | integration, volume | medium | specifications, support channels | keep informed |
| Auditors and assessors | evidence | medium | documents, access, traceability | consult early |
| End users (mobile) | simple, accessible, safe payments | low (direct) | usable app, clear errors | usability and accessibility testing |
| Regulators | compliance | indirect | via the compliance pack | via compliance lead |

## 2. Communication matrix
| What | Audience | Frequency | Channel | Owner |
|---|---|---|---|---|
| Stand-up | team | daily, 15 min | chat or call | tech leads |
| Weekly status (progress, RAG, risks, decisions needed) | sponsor, product owner | weekly | one-page report | program manager |
| Sprint demo | product owner, pilot customer | every 2 weeks | live demo | product owner |
| Monthly cost and EVM review | sponsor | monthly | workbook + 30-min review | program manager |
| Risk review | program manager, leads | weekly | register | program manager |
| Phase gate review (M1-M6) | sponsor, product owner, leads | six times | meeting + gate checklist | program manager |
| Security findings update | sponsor, security lead | per finding, weekly in W19-22 | secure channel | security lead |
| Customer pilot sync | pilot customer | weekly from W20 | call | product owner |
| Release notes and change log | all | each release | repository and email | engineering lead |
| Incident notice | sponsor, affected parties | within [1] hour of Sev 1 | phone then email | on-call lead |

## 3. Status report content
Overall RAG; schedule (SPI) and cost (CPI, EAC); milestones next 30 days; top five risks; decisions needed with deadline; change requests; quality (open defects by severity, test results); next period plan. Thresholds: green at or above 0.95, amber 0.90-0.95, red below 0.90.

## 4. Escalation path
Team member -> tech lead (same day) -> program manager (1 working day) -> sponsor (2 working days). Security or data incidents skip levels and go to the security lead and sponsor at once.

## 5. Engagement plan by phase
P1: kick-off with sponsor and leads; agree success measures and decision rights. P2-P3: demos to the product owner; pilot-customer discovery interviews. P4: pilot customer sees the client and log viewer; usability and accessibility sessions with users who use assistive technology. P5: auditors briefed; evidence walkthroughs. P6: UAT, training for customer administrators, go-live communication, hypercare contact list.

## 6. Feedback and adaptation
Survey the sponsor and product owner after each gate (three questions: clear, on time, useful). Pilot customer feedback is logged and triaged weekly. The plan is revised when a stakeholder's influence or interest changes.

## 7. Decision rights (RACI summary)
| Decision | Sponsor | Product owner | Program manager | Architect | Security lead |
|---|---|---|---|---|---|
| Scope change over 1 week or $10,000 | A | C | R | C | C |
| Priority of backlog | I | A | C | C | I |
| Architecture change | I | C | I | A | C |
| Accept security finding as residual risk | A | C | I | C | R |
| Go-live | A | R | R | C | C |
(R responsible, A accountable, C consulted, I informed)

## 8. Confidentiality
Customer and security information is shared on a need-to-know basis; no production data in non-production channels; pen-test reports are restricted to named recipients.
