#!/usr/bin/env bash
# One-time setup: creates the solution, restores, builds, runs unit tests.
set -euo pipefail
cd "$(dirname "$0")/../backend"
[ -f SentinelPay.sln ] || { { dotnet new sln -n SentinelPay --format sln || dotnet new sln -n SentinelPay; }; dotnet sln add $(find src tests -name '*.csproj'); }
dotnet restore && dotnet build --no-restore -c Release
dotnet test tests/SentinelPay.Domain.Tests --no-build; dotnet test tests/SentinelPay.Application.Tests --no-build; dotnet test tests/SentinelPay.Agents.Tests --no-build
echo "Unit tests passed. Integration tests need Docker:  dotnet test tests/SentinelPay.Integration.Tests"
echo "Create the first migration:  dotnet ef migrations add Initial -p src/SentinelPay.Infrastructure -s src/SentinelPay.Api"
