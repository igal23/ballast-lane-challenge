#!/usr/bin/env bash
# Run ONCE from the repository root, after design approval.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"
bash scripts/check-prereqs.sh

if [[ -e TaskBoard.slnx || -e TaskBoard.sln ]]; then
  echo "Solution already exists. Refusing to overwrite. Do not rerun this script." >&2
  exit 1
fi
if [[ -e frontend/taskboard-web/angular.json ]]; then
  echo "Angular app already exists. Refusing to overwrite." >&2
  exit 1
fi

mkdir -p src tests frontend

dotnet new sln -n TaskBoard

dotnet new classlib -n TaskBoard.Domain -o src/TaskBoard.Domain -f net10.0
dotnet new classlib -n TaskBoard.Application -o src/TaskBoard.Application -f net10.0
dotnet new classlib -n TaskBoard.Infrastructure -o src/TaskBoard.Infrastructure -f net10.0
dotnet new webapi -n TaskBoard.Api -o src/TaskBoard.Api -f net10.0 --use-controllers
dotnet new xunit -n TaskBoard.UnitTests -o tests/TaskBoard.UnitTests -f net10.0
dotnet new xunit -n TaskBoard.IntegrationTests -o tests/TaskBoard.IntegrationTests -f net10.0

for project in   src/TaskBoard.Domain/TaskBoard.Domain.csproj   src/TaskBoard.Application/TaskBoard.Application.csproj   src/TaskBoard.Infrastructure/TaskBoard.Infrastructure.csproj   src/TaskBoard.Api/TaskBoard.Api.csproj   tests/TaskBoard.UnitTests/TaskBoard.UnitTests.csproj   tests/TaskBoard.IntegrationTests/TaskBoard.IntegrationTests.csproj; do
  dotnet sln add "$project"
done

dotnet add src/TaskBoard.Application/TaskBoard.Application.csproj reference src/TaskBoard.Domain/TaskBoard.Domain.csproj
dotnet add src/TaskBoard.Infrastructure/TaskBoard.Infrastructure.csproj reference src/TaskBoard.Application/TaskBoard.Application.csproj
dotnet add src/TaskBoard.Api/TaskBoard.Api.csproj reference src/TaskBoard.Application/TaskBoard.Application.csproj
dotnet add src/TaskBoard.Api/TaskBoard.Api.csproj reference src/TaskBoard.Infrastructure/TaskBoard.Infrastructure.csproj
dotnet add tests/TaskBoard.UnitTests/TaskBoard.UnitTests.csproj reference src/TaskBoard.Application/TaskBoard.Application.csproj
dotnet add tests/TaskBoard.IntegrationTests/TaskBoard.IntegrationTests.csproj reference src/TaskBoard.Api/TaskBoard.Api.csproj

# Angular creates its own package.json and installs app dependencies.
npx --yes @angular/cli@22 new taskboard-web   --directory frontend/taskboard-web   --routing --style=scss --skip-git --defaults

echo "Bootstrap completed. Run 'dotnet test TaskBoard.slnx' and build Angular in frontend/taskboard-web."
