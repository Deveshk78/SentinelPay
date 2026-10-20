# First real build: what to run, what will probably break, how to fix it

**Status:** nothing in this repository has ever been compiled or run. The authoring environment had no .NET, Flutter, Docker or network. Expect failures on the first run; the order below is the order they are most likely to appear.

## Run it
```bash
./scripts/bootstrap.sh                                   # solution, restore, build, unit tests
dotnet test backend/tests/SentinelPay.Integration.Tests  # needs Docker
cd client && flutter pub get && flutter analyze --no-fatal-infos && flutter test
dotnet build backend/src/SentinelPay.Agents -p:UseMaf=true   # Agent Framework flavour, separately
```
Or push to GitHub: `.github/workflows/ci.yml` runs the same, uploads logs, and runs the Agent Framework build as a separate job that is allowed to fail.

## Why the Agent Framework cannot block you
`SentinelPay.Agents` has two flavours. The default build uses only `Microsoft.Extensions.AI` (`IChatClient`) and runs the two risk agents in parallel; every test, including the agent-hardening tests, compiles against it. The Agent Framework graph is behind `-p:UseMaf=true`. Get the default green first, then work on the framework build.

## Likely failures, in order
| # | Where | Symptom | Likely cause and fix |
|---|---|---|---|
| 1 | restore | NU1101 / NU1102 package or version not found | versions in `Directory.Packages.props` are starting points; run `dotnet list package --outdated`, pick a real version. Check `Microsoft.Extensions.AI.OpenAI` (the `AsIChatClient` extension name changed across previews) and the `Microsoft.Agents.*` preview range |
| 2 | Infrastructure | RabbitMQ.Client errors | v7 API: check signatures of `BasicPublishAsync`, `CreateChannelAsync`, `BasicConsumeAsync` against the installed version |
| 3 | Api | `AsIChatClient` not found | use the extension name your `Microsoft.Extensions.AI.OpenAI` version provides (`AsChatClient` in older previews) |
| 4 | Domain / Application | colour-colour or nullable errors | warnings are not errors by default now; real errors are usually a missing `using` |
| 5 | tests | NSubstitute or EF InMemory configuration | `TestDb` has no model config; if EF complains about a key or constructor, add `OnModelCreating` mapping |
| 6 | Integration | `column "xmin" ... conflicts with a system column` at `EnsureCreated` | map the concurrency token with the Npgsql-documented approach for your provider version, or use migrations; see `TEST_FINDINGS.md` item 5 |
| 7 | Integration | containers fail to start | Docker not running or no pull access; set a registry mirror |
| 8 | MAF flavour | `Executor<,>`, `WorkflowBuilder`, `AddFanOutEdge`, `AddFanInEdge`, `InProcessExecution.RunAsync`, `WorkflowOutputEvent.As<T>`, `YieldOutputAsync`, `ChatClientAgent.RunAsync` not found or different signature | the block under `#if USE_MAF` in `RiskGraph.cs` and `AgentRunner` is the only code to touch; compare with the current Agent Framework samples and adjust names |
| 9 | client | `flutter analyze` infos | CI uses `--no-fatal-infos`; fix real warnings only |
| 10 | client tests | widget test overflows at 200% text | that is a real accessibility defect: fix the layout, do not loosen the test |

## Tighten after green
Run `dotnet build -p:StrictWarnings=true` and fix warnings; pin the `Microsoft.Agents.*` versions exactly; replace placeholder pins and the symmetric JWT key; then run the fault-injection runbook.

## What to send back
`build.log` (CI artifact) or `dotnet build 2>&1 | grep -E "error|warn" | sort -u`, plus the first failing test output. Fix in this order: restore, build, unit tests, integration, client, MAF.
