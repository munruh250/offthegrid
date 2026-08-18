#!/bin/bash
set -e

cd "$(dirname "$0")/.."

echo "Building LastOut.Sim..."
dotnet build src/LastOut.Sim -c Release -warnaserror

echo ""
echo "Running tests..."
dotnet test src/LastOut.Tests -c Release --logger "console;verbosity=minimal" --no-build

echo ""
echo "Checking for UnityEngine references in Sim .cs files..."
if grep -r "using UnityEngine" src/LastOut.Sim/*.cs src/LastOut.Sim/**/*.cs 2>/dev/null; then
  echo "ERROR: LastOut.Sim must not reference UnityEngine"
  exit 1
fi

echo "✓ Sim verification passed"
