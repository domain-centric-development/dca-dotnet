# Releasing

Three packages go to NuGet.org under the reserved prefix `DomainCentric`, in two independently versioned
families with one tag each: `DomainCentric.BuildingBlocks` (`building-blocks/vX.Y.Z`) and
`DomainCentric.ArchRules` + `DomainCentric.ArchRules.Xunit`, released together (`archrules/vX.Y.Z`).

**Publishing runs on the maintainer's machine**, via [scripts/release.sh](scripts/release.sh). The NuGet
API key stays there — CI never sees it, the same decision as for the Java libraries. GitHub Actions builds
and tests every push, and on a release tag it verifies the version is on NuGet.org and creates the GitHub
release ([release.yml](.github/workflows/release.yml)).

## One-time setup

### 1. NuGet.org account and prefix

1. Account on [nuget.org](https://www.nuget.org) (Microsoft account).
2. The package ID prefix `DomainCentric` is reserved *after* the first package exists: push
   `DomainCentric.BuildingBlocks` first, then request the reservation (Manage Package ID Prefixes). Until
   the reservation is granted the packages show no verified-prefix badge; nothing else changes.
3. Create an **API key** (Account → API Keys): push new packages and package versions, glob pattern
   `DomainCentric.*`, the shortest expiry you can live with (a year at most — NuGet.org expires them).

The key is deliberately **not** kept in `~/.nuget/NuGet.Config` or the shell profile — both are
world-readable plaintext for every process running as you. `scripts/release.sh` looks for it in three
places, in order.

**Keychain (recommended).** Stored once, unlocked per access by macOS:

```bash
security add-generic-password -s dca-nuget -a nuget.org -w
# paste the API key
security find-generic-password -s dca-nuget -w    # check
```

Add `-T ""` to the `add` call to force a confirmation dialog on every read, or
`security delete-generic-password -s dca-nuget` to remove it again.

**Environment**, for one shell session:

```bash
read -rs -p 'NuGet API key: ' k && echo && export NUGET_API_KEY="$k" && unset k
```

**Prompt.** With neither of the above, the script asks for the key and keeps it in its own process only.

### 2. Signing

Nothing to set up: NuGet.org signs every package with its repository certificate on upload, and
consumers verify that signature. Author signing (a code-signing certificate) is not used.

## Releasing a package family

Versions live in the projects' `<Version>` elements, not on the command line: a `-p:Version=…` handed
to `dotnet pack` is a global property that would also re-version the referenced
`DomainCentric.BuildingBlocks` and turn the packaged dependency into a version that does not exist.
So a release starts with a commit that sets `<Version>` and the changelog:

1. `<Version>X.Y.Z</Version>` in the family's project(s) — both ArchRules projects for `archrules`.
2. `## [X.Y.Z] - YYYY-MM-DD` section at the top of the family's `CHANGELOG.md`.
3. Commit. Then:

```bash
./scripts/release.sh building-blocks 0.1.0     # or: archrules 0.1.0
```

The script refuses to continue unless everything is in order, then builds, tests, packs, inspects the
packages, asks for a typed confirmation and pushes:

1. clean working tree, tag not taken, `## [X.Y.Z]` section present, every project of the family at
   `<Version>X.Y.Z</Version>`
2. for `archrules`: `DomainCentric.BuildingBlocks` is at a release version **and that version is on
   NuGet.org** — it becomes the package dependency
3. API key from environment, keychain or prompt ([scripts/lib/nuget-key.sh](scripts/lib/nuget-key.sh))
4. `dotnet build` and `dotnet test` (Debug — the architecture self-tests need it), rule catalog current
5. `dotnet pack -c Release` per project; every `.nupkg` must contain `LICENSE`, `README.md`, `icon.png`
   and the nuspec, with a `.snupkg` next to it
6. `dotnet nuget push … --skip-duplicate` per package (the symbol package goes along). A pushed version
   can be **unlisted but never deleted or replaced** — that is why the typed confirmation sits in front

Afterwards, as the script prints:

1. Wait until NuGet.org lists the packages (indexing takes a few minutes).
2. Tag it:

   ```bash
   git tag building-blocks/v0.1.0
   git push origin building-blocks/v0.1.0
   ```

   The tag comes **after** the push, so the workflow finds the packages on NuGet.org instead of waiting
   for a push that has not happened.
3. Open the next version: bump `<Version>` in the released projects and start an `[Unreleased]` section.

Order for a first release: `building-blocks` first, then `archrules` — the ArchRules preflight checks
that the BuildingBlocks version it depends on is already served.

## Manual equivalent

The script wraps these commands; nothing stops you from running them directly:

```bash
dotnet build && dotnet test --no-build
dotnet pack src/DomainCentric.BuildingBlocks/DomainCentric.BuildingBlocks.csproj -c Release -o artifacts
dotnet nuget push artifacts/DomainCentric.BuildingBlocks.0.1.0.nupkg --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json --skip-duplicate
```

## Trying a package before releasing it

```bash
dotnet pack -c Release -o artifacts
dotnet nuget add source "$PWD/artifacts" --name dca-local        # once
cd ../some-consumer && dotnet add package DomainCentric.ArchRules.Xunit --version 0.1.0 --source dca-local
```

`samples/MinimalConsumer` is the smallest such consumer; it builds against the project references here.
