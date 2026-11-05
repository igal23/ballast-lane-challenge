#!/usr/bin/env bash
set -euo pipefail

missing=0
for tool in dotnet node npm git docker; do
  if ! command -v "$tool" >/dev/null 2>&1; then
    echo "Missing: $tool" >&2
    missing=1
  fi
done
if (( missing != 0 )); then
  exit 1
fi

sdk_version="$(dotnet --version)"
if [[ "${sdk_version%%.*}" != "10" ]]; then
  echo "Expected .NET SDK 10; found $sdk_version" >&2
  exit 1
fi

node_version="$(node -p 'process.versions.node')"
node -e '
const [major, minor, patch] = process.versions.node.split(".").map(Number);
const ok = (major === 22 && (minor > 22 || (minor === 22 && patch >= 3)))
  || (major === 24 && minor >= 15)
  || major === 26;
if (!ok) { console.error(`Angular 22 requires Node ^22.22.3, ^24.15.0 or ^26.0.0; found ${process.versions.node}`); process.exit(1); }
'

if ! docker compose version >/dev/null 2>&1; then
  echo "Docker Compose plugin is required" >&2
  exit 1
fi

echo "Ready: .NET $sdk_version; Node $node_version; Docker Compose available."
