#!/usr/bin/env bash
#
# Releases one package family to NuGet.org from this machine.
#
#   ./scripts/release.sh building-blocks 0.1.0     # DomainCentric.BuildingBlocks
#   ./scripts/release.sh archrules 0.1.0           # DomainCentric.ArchRules + DomainCentric.ArchRules.Xunit
#
# The API key is never stored in a build file. It is taken from, in order:
#   1. NUGET_API_KEY in the environment
#   2. the macOS keychain item "dca-nuget" (see RELEASING.md for how to store it)
#   3. an interactive prompt
#
# Versions live in the projects' <Version> elements — not on the command line: a -p:Version=… passed to
# `dotnet pack` is a global property and would also re-version the referenced DomainCentric.BuildingBlocks,
# turning the packaged dependency into a version that does not exist.
#
set -euo pipefail

cd "$(dirname "$0")/.."

die() { echo "error: $*" >&2; exit 1; }

[ $# -eq 2 ] || die "usage: $0 <building-blocks|archrules> <version>"
FAMILY="$1"
VERSION="$2"

case "$FAMILY" in
  building-blocks) PROJECTS=(src/DomainCentric.BuildingBlocks/DomainCentric.BuildingBlocks.csproj)
                   CHANGELOG=src/DomainCentric.BuildingBlocks/CHANGELOG.md ;;
  archrules)       PROJECTS=(src/DomainCentric.ArchRules/DomainCentric.ArchRules.csproj
                             src/DomainCentric.ArchRules.Xunit/DomainCentric.ArchRules.Xunit.csproj)
                   CHANGELOG=src/DomainCentric.ArchRules/CHANGELOG.md ;;
  *) die "unknown package family '$FAMILY' (building-blocks|archrules)" ;;
esac

[[ "$VERSION" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[A-Za-z0-9.]+)?$ ]] || die "not a release version: $VERSION"

project_version() { sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$1"; }
package_id()      { sed -n 's:.*<PackageId>\(.*\)</PackageId>.*:\1:p' "$1"; }
lower()           { printf '%s' "$1" | tr '[:upper:]' '[:lower:]'; }
on_nuget()        { curl -fsSL -o /dev/null "https://api.nuget.org/v3-flatcontainer/$(lower "$1")/$2/$(lower "$1").$2.nupkg"; }

# --- preflight -------------------------------------------------------------------------------------

command -v dotnet >/dev/null || die "dotnet SDK not on PATH"
command -v unzip  >/dev/null || die "unzip not on PATH (used to inspect the packed .nupkg)"
command -v curl   >/dev/null || die "curl not on PATH (used to query NuGet.org)"
[ -z "$(git status --porcelain)" ] || die "working tree is dirty — commit the changelog first"

git rev-parse -q --verify "refs/tags/$FAMILY/v$VERSION" >/dev/null \
  && die "tag $FAMILY/v$VERSION already exists"

grep -q "^## \[$VERSION\]" "$CHANGELOG" || die "$CHANGELOG has no '## [$VERSION]' section"

for p in "${PROJECTS[@]}"; do
  PV="$(project_version "$p")"
  [ "$PV" = "$VERSION" ] || die "$p has <Version>$PV</Version> — set it to $VERSION and commit"
done

if [ "$FAMILY" = "archrules" ]; then
  BB=src/DomainCentric.BuildingBlocks/DomainCentric.BuildingBlocks.csproj
  BB_VERSION="$(project_version "$BB")"
  case "$BB_VERSION" in
    *-*|"") die "DomainCentric.BuildingBlocks is at '$BB_VERSION' — release it first, it becomes the package dependency" ;;
  esac
  on_nuget DomainCentric.BuildingBlocks "$BB_VERSION" \
    || die "DomainCentric.BuildingBlocks $BB_VERSION is not on NuGet.org yet — the ArchRules package would depend on it"
  echo "DomainCentric.ArchRules $VERSION will depend on DomainCentric.BuildingBlocks $BB_VERSION"
fi

# --- NuGet API key ---------------------------------------------------------------------------------

# shellcheck source=lib/nuget-key.sh
. scripts/lib/nuget-key.sh

# --- build, test, pack -----------------------------------------------------------------------------

echo "==> build and test (Debug — the architecture self-tests need it)"
dotnet build
dotnet test --no-build

echo "==> rule catalog is current"
dotnet run --project tools/RulesCatalog --no-build -- .
git diff --exit-code -- rules.json RULES.md || die "rules.json / RULES.md are stale — regenerate and commit"

echo "==> pack $FAMILY $VERSION"
rm -rf artifacts
for p in "${PROJECTS[@]}"; do
  dotnet pack "$p" -c Release -o artifacts
done

for p in "${PROJECTS[@]}"; do
  ID="$(package_id "$p")"
  NUPKG="artifacts/$ID.$VERSION.nupkg"
  [ -f "$NUPKG" ] || die "$NUPKG was not produced"
  for f in LICENSE README.md icon.png "$ID.nuspec"; do
    unzip -l "$NUPKG" | grep -q " $f\$" || die "$NUPKG lacks $f"
  done
  [ -f "artifacts/$ID.$VERSION.snupkg" ] || die "no symbol package for $ID"
  echo "$NUPKG: licence, readme, icon, symbols present"
done

echo
echo "About to push to NuGet.org — a pushed version can be unlisted, never deleted or replaced:"
ls -1 artifacts/*.nupkg
read -r -p "type the version to confirm: " CONFIRM
[ "$CONFIRM" = "$VERSION" ] || die "aborted"

for p in "${PROJECTS[@]}"; do
  ID="$(package_id "$p")"
  # Pushes the .snupkg found next to the .nupkg as well.
  dotnet nuget push "artifacts/$ID.$VERSION.nupkg" --api-key "$NUGET_API_KEY" \
    --source https://api.nuget.org/v3/index.json --skip-duplicate
done

# --- afterwards ------------------------------------------------------------------------------------

echo
echo "pushed. NuGet.org validates and indexes the packages (a few minutes):"
for p in "${PROJECTS[@]}"; do
  echo "  https://www.nuget.org/packages/$(package_id "$p")/$VERSION"
done
echo
echo "next:"
echo "  1. wait until the packages are listed (the release workflow polls the same URL for 30 minutes)"
echo "  2. git tag $FAMILY/v$VERSION && git push origin $FAMILY/v$VERSION"
echo "  3. open the next version: bump <Version> in the released projects and start an [Unreleased] section"
