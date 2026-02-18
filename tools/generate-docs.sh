#!/bin/bash

set -e

REPO_ROOT="$(dirname "$0")/.."
CLIENT_DIR="$REPO_ROOT/src/ClassicUO.Client"
API_TO_MARKDOWN="$REPO_ROOT/src/APIToMarkdown/APIToMarkdown.csproj"
DOCS_OUTPUT="$CLIENT_DIR/LegionScripting/docs"

SOURCE_FILES=()
while IFS= read -r -d '' f; do
    SOURCE_FILES+=("$f")
done < <(find "$CLIENT_DIR/LegionScripting/ApiClasses" -name "*.cs" -print0)
SOURCE_FILES+=("$CLIENT_DIR/LegionScripting/LegionAPI.cs")

if [ ${#SOURCE_FILES[@]} -eq 0 ]; then
    echo "Error: No source files found for doc generation."
    exit 1
fi

echo "Generating docs into: $DOCS_OUTPUT"
echo "Source files: ${#SOURCE_FILES[@]} files"

dotnet run --project "$API_TO_MARKDOWN" "$DOCS_OUTPUT" "${SOURCE_FILES[@]}"

echo "Docs generated successfully."
