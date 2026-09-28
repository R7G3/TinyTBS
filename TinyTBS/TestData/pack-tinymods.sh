#!/usr/bin/env bash
# Rebuild TestData/Tinymods/*.tinymod.zip from Modules/ (module.json at zip root).
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
MODULES_ROOT="$HERE/Modules"
TINYMODS_ROOT="$HERE/Tinymods"

if [[ ! -d "$MODULES_ROOT" ]]; then
  echo "Modules folder not found: $MODULES_ROOT" >&2
  exit 1
fi

if ! command -v zip >/dev/null 2>&1; then
  echo "zip is required (apt install zip / brew install zip)" >&2
  exit 1
fi

mkdir -p "$TINYMODS_ROOT"

for module_id in test_theme test_units test_buildings test_scenario; do
  module_dir="$MODULES_ROOT/$module_id"
  if [[ ! -d "$module_dir" ]]; then
    echo "Missing module folder: $module_dir" >&2
    exit 1
  fi

  zip_path="$TINYMODS_ROOT/${module_id}.tinymod.zip"
  rm -f "$zip_path"
  (
    cd "$module_dir"
    zip -r -q "$zip_path" .
  )
  echo "Packed $module_id -> $zip_path"
done

echo "Done."
