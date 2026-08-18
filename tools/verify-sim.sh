#!/bin/bash
set -e

cd "$(dirname "$0")/.."

echo "Building OffTheGrid.Sim..."
dotnet build src/OffTheGrid.Sim -c Release -warnaserror

echo ""
echo "Running tests..."
dotnet test src/OffTheGrid.Tests -c Release --logger "console;verbosity=minimal" --no-build

echo ""
echo "Checking for UnityEngine references in Sim .cs files..."
if grep -r "using UnityEngine" src/OffTheGrid.Sim/*.cs src/OffTheGrid.Sim/**/*.cs 2>/dev/null; then
  echo "ERROR: OffTheGrid.Sim must not reference UnityEngine"
  exit 1
fi

echo "✓ Sim verification passed"
