#!/bin/bash

# ClassicUO Development Runner
# Fast development workflow without NativeAOT compilation

set -e

SCRIPT_DIR="$(dirname "$0")"
PROJECT_ROOT="."
# SETTINGS_PATH="/Users/forrrest/projects/UO-BritainKnights/Game/settings.json"
# SETTINGS_PATH="/Users/forrrest/projects/OpenUO/settings/settings_osi.json"
# SETTINGS_PATH="/Users/forrrest/projects/UO-Adventures-Dev/ClassicUO/settings.json"
SETTINGS_PATH="/Users/forrrest/TazUO-Launcher.osx-arm64/Profiles/Settings/a730bff5-6701-46e7-b054-22d13c0f92e7.json"

# Debug flags
DEBUG_GUMP_LOADING="${DEBUG_GUMP_LOADING:-false}"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --debug-gump-loading)
            DEBUG_GUMP_LOADING="true"
            shift
            ;;
        *)
            # Pass through other arguments
            break
            ;;
    esac
done

cd "./src/ClassicUO.Client"

echo "=========================================="
echo "ClassicUO - Development Mode"
echo "=========================================="
echo ""
echo "Running with .NET runtime (fast iteration)"
echo "- Architecture: $(uname -m) native"
echo "- Fast startup, full debugging support"
echo "- Settings: $SETTINGS_PATH"
echo "- Debug Gump Loading: $DEBUG_GUMP_LOADING"
echo ""
echo "Note: Plugin system (cuoapi.dll) may not work on arm64"
echo "      Game will run without plugins"
echo ""
echo "Press Ctrl+C to stop"
echo ""

# Export debug flag as environment variable
export DEBUG_GUMP_LOADING

# Run with .NET runtime directly
dotnet run -c Debug -- -settings "$SETTINGS_PATH" "$@"