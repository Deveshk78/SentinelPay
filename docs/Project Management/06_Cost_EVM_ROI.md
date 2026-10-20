# Project Cost, Earned Value and ROI

Source workbook: `SentinelPay_Cost_EVM_ROI_Model.xlsx` (formulas live; change yellow cells to re-run). **All rates, prices and customer volumes are my placeholder assumptions in US dollars.** Replace them before using any figure for a decision.

## 1. Cost baseline
| Item | Cost ($) |
|---|---|
| Labor (9 roles, 63.25 FTE-months) | 536,250 |
| Cloud dev/test (6 x $3,000) | 18,000 |
| Tooling and licences (6 x $2,000) | 12,000 |
| Legal and privacy review | 10,000 |
| Penetration test | 25,000 |
| Compliance audit preparation | 30,000 |
| Documentation and training | 8,000 |
| **Subtotal** | **639,250** |
| Contingency 10% | 63,925 |
| **Cost baseline (BAC)** | **703,175** |
| Management reserve 5% (sponsor-held) | 35,159 |
| **Total funding** | **738,334** |

Labor by role ($): program manager 54,000; principal architect 70,000; backend engineers 120,000; Flutter engineers 75,000; AI/agent engineer 45,000; QA/SDET 57,000; DevOps/SRE 54,000; security and compliance 35,000; UX and accessibility 26,250.

## 2. Planned value (the S-curve)
| Month | 1 | 2 | 3 | 4 | 5 | 6 |
|---|---|---|---|---|---|---|
| Monthly baseline ($) | 84,700 | 119,900 | 117,150 | 121,000 | 166,650 | 93,775 |
| Cumulative PV ($) | 84,700 | 204,600 | 321,750 | 442,750 | 609,400 | 703,175 |

Month 5 is the peak because the pen-test and compliance preparation land there.

## 3. Earned value management
Each month: record **% complete** (from work packages with 0/50/100 rules, not opinion) and **actual cost (AC)**. Then EV = % x BAC.

| Measure | Formula | Meaning |
|---|---|---|
| CV, SV | EV-AC, EV-PV | cost and schedule variance in dollars |
| CPI, SPI | EV/AC, EV/PV | efficiency; below 1.00 is bad |
| EAC | BAC/CPI | forecast total cost if efficiency holds |
| ETC, VAC | EAC-AC, BAC-EAC | cost still to come; forecast over/under |
| TCPI | (BAC-EV)/(BAC-AC) | efficiency needed on the remaining work |

**Worked example (ILLUSTRATIVE numbers in the workbook, not real progress).** At month 3, PV $321,750, EV $315,022 (44.8% complete), AC $328,200. CPI 0.96, SPI 0.98, EAC $732,589, VAC -$29,414 (a forecast overrun about 4%), TCPI 1.035. That example would be amber-leaning and still inside the contingency of $63,925. **Thresholds:** amber below 0.95, red below 0.90; red triggers a recovery plan to the sponsor.

## 4. Return on investment
The workbook models 36 months: six months of build, then selling.

**Base-case assumptions (placeholders):** subscription $150,000 per customer per year; onboarding fee $60,000 per customer; run cost $2,500 per customer per month; post-launch team $70,000 per month; sales and marketing $25,000 per month from month 5; new customers 1 per month in months 7-12, 2 per month in months 13-24, 3 per month in months 25-36; 5% annual churn; 12% discount rate.

| Result (base case) | Value |
|---|---|
| Build investment incl. reserve | $738,334 |
| First month with positive net cash | **month 10** |
| **Payback month (cumulative cash turns positive)** | **month 18** (12 months after go-live) |
| Peak funding need | $833,504 |
| Net cash at month 36 | $8.46 million |
| ROI over 36 months | 1,146% |
| NPV at 12% | $6.35 million |
| Active customers, month 36 | about 63 |

### When does the return start?
1. **Months 1-6: spending only.** No return exists until the product is live.
2. **Month 7: first customer deploys.** The onboarding fee arrives, but the sustain team and sales cost exceed revenue, so cash is still negative.
3. **Month 10: monthly cash turns positive** (base case). Each month now earns more than it costs.
4. **Month 18: the investment is paid back** in cumulative terms.
5. **After month 18: profit accumulates** and grows with each added customer.

A single customer contributes about $60,000 on deploy plus $150,000 a year less $30,000 run cost, roughly **$180,000 in its first year**. Delivery effort for onboarding is assumed to be covered by the sustain team, which the model does not cost separately: this flatters the result.

### Sensitivity to customer wins (snapshot, adoption factor)
| Adoption | Payback month | Net cash month 36 | NPV | Peak funding | Customers month 36 |
|---|---|---|---|---|---|
| 1.0 (base) | 18 | $8.46M | $6.35M | $0.83M | 63 |
| 0.5 | 27 | $2.41M | $1.63M | $1.07M | 31 |
| 0.25 | not within 36 months | -$0.61M | -$0.73M | $1.45M | 16 |

**Read this honestly:** the base-case ROI is large mostly because the model assumes strong, steady customer wins at a $150,000 price. At a quarter of the assumed adoption the product does not pay back in three years. The break-even adoption lies between 25% and 50% of base. Your first job is to test price and customer pipeline with real buyers; the cost side (about $0.7 million) is the more certain half.

## 5. Funding and controls
Release funding in tranches at milestones M1, M3 and M5 [percentages to be agreed]. Contingency is used by the program manager within the charter limits; reserve only by the sponsor. Forecasts are refreshed monthly in the workbook and reported in the status pack.

## 6. Limits of the model
No inflation or currency effects; no tax; no gateway revenue share; no refunds or chargebacks cost; costs of customer-specific integrations are not separated; revenue timing assumes monthly billing. Add these before using the figures for an investment decision.
