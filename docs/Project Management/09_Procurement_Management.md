# Procurement Management Plan

## 1. What we buy, and why
| Need | Make or buy | Contract type | Needed by | Budget line |
|---|---|---|---|---|
| Core team (architect, engineers, QA) | Make (employees or contractors) | employment or T&M contractors | W1 | Labor |
| Penetration test | Buy | fixed price | book by W10, run W19-21 | $25,000 |
| Compliance readiness assessor | Buy | fixed price | W18-22 | $30,000 |
| Legal and privacy review (DPIA, terms) | Buy | fixed fee or capped hours | W6-10 | $10,000 |
| Azure subscription (Front Door, App Gateway, Service Bus, Key Vault, OpenAI) | Buy | pay-as-you-go, committed spend later | W2 | cloud and run cost |
| AWS standby account | Buy | pay-as-you-go | W2 | cloud |
| Payment gateway(s) | Buy | revenue-share or per-transaction | sandbox W5, production W22 | pass-through |
| Identity provider | Buy | subscription | W6 | tooling |
| CI/CD, scanners, observability | Buy | subscription | W1 | $2,000/month |
| Accessibility audit | Buy | fixed price | W17 | inside compliance line |
| Training and documentation tooling | Buy | subscription | W20 | $8,000 |

If the delivery partner route in document 01 is chosen, the core-team line is replaced by a fixed-price or capped T&M contract and must still fit the baseline.

## 2. Process
1. Define requirements and evaluation criteria; 2. approved-vendor or market check; 3. issue RFP or request quotes (three where practical); 4. score against weighted criteria; 5. legal and security review (data-processing terms, sub-processors, breach notification); 6. approval by the authority in the charter; 7. contract and onboarding; 8. performance review and payment on acceptance; 9. close-out.

## 3. Selection criteria (default)
Capability and relevant experience 30%; security and compliance posture 25%; price 20%; delivery capacity and dates 15%; references 10%. Security vendors must be independent of the delivery team.

## 4. Contract terms to insist on
Acceptance criteria tied to payment; IP assignment; confidentiality and data-protection terms; right to audit; incident notification within [24] hours; no training of third-party models on our data; exit and data return; liability caps proportionate to risk; subcontractor consent.

## 5. Supply-chain security
Pinned package versions and an SBOM; dependencies adopted only after a minimum age policy; vulnerability scanning in CI; cloud access through managed identities; secrets in Key Vault; third-party SDKs reviewed for permissions and data collection.

## 6. Vendor management
A named owner per vendor; monthly review for the cloud and gateway vendors; a vendor risk register entry for each; sandbox and production credentials separated and rotated.

## 7. Authority and budget control
Purchases up to $10,000 inside the baseline: program manager. Above that or outside the baseline: sponsor. Each commitment is recorded against the cost model and flagged in the monthly cost review.

## 8. Procurement risks
Vendor delay (pen-test slot): book by W10. Gateway onboarding delay: stub gateway and early application. Cost overrun on cloud or model usage: budgets and alarms. Vendor lock-in: ports and adapters keep gateways, brokers and agent engine swappable.
