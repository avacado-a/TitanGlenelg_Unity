#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"

echo "=== Compiling Unity Stubs ==="
mcs -target:library -langversion:latest -out:"${SCRIPT_DIR}/stubs/UnityStubs.dll" "${SCRIPT_DIR}/stubs/UnityStubs.cs"

echo "=== Compiling TitanGlenelg Core DLL ==="
CORE_FILES=$(find "${ROOT_DIR}/Assets/Scripts" -name "*.cs")
mcs -target:library -langversion:latest \
    -r:"${SCRIPT_DIR}/stubs/UnityStubs.dll" \
    -out:"${SCRIPT_DIR}/stubs/TitanGlenelg.Core.dll" \
    ${CORE_FILES}

echo "=== Compiling TitanGlenelg Tests DLL ==="
TEST_FILES=$(find "${ROOT_DIR}/Assets/Tests" -name "*.cs")
mcs -target:library -langversion:latest \
    -r:"${SCRIPT_DIR}/stubs/UnityStubs.dll" \
    -r:"${SCRIPT_DIR}/stubs/TitanGlenelg.Core.dll" \
    -out:"${SCRIPT_DIR}/stubs/TitanGlenelg.Tests.dll" \
    ${TEST_FILES}

echo "=== Compiling Test Runner Executable ==="
mcs -target:exe -langversion:latest \
    -r:"${SCRIPT_DIR}/stubs/UnityStubs.dll" \
    -r:"${SCRIPT_DIR}/stubs/TitanGlenelg.Core.dll" \
    -r:"${SCRIPT_DIR}/stubs/TitanGlenelg.Tests.dll" \
    -out:"${SCRIPT_DIR}/stubs/RunTests.exe" \
    "${SCRIPT_DIR}/stubs/RunTests.cs"

echo "=== Running Native C# NUnit Test Suite ==="
mono "${SCRIPT_DIR}/stubs/RunTests.exe"

echo "=== Running Python Verification Suite ==="
python3 "${SCRIPT_DIR}/verify_systems.py"

echo "=== All Tests Succeeded! ==="
