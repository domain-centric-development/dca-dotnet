# Releasing

Three packages go to NuGet.org under the prefix `DomainCentric`, in two independently versioned
families with one tag each: `DomainCentric.BuildingBlocks` (`building-blocks/vX.Y.Z`) and
`DomainCentric.ArchRules` + `DomainCentric.ArchRules.Xunit`, released together (`archrules/vX.Y.Z`).

**A pushed tag publishes.** [release.yml](.github/workflows/release.yml) verifies the tagged commit
(project versions, changelog, build, tests, catalog), packs, and pushes to NuGet.org through **Trusted
Publishing**: the job presents its GitHub OIDC token, NuGet.org checks it against a policy naming this
repository, this workflow file and the `nuget` environment, and answers with an API key that lives for
the job. No long-lived credential exists — not in GitHub secrets, not on a machine. The workflow then
waits until NuGet.org lists the packages and creates the GitHub release.

[scripts/release.sh](scripts/release.sh) is the **fallback** for publishing from a machine (NuGet.org
still accepts API keys for command-line pushes); it runs the same checks and needs an API key.

## One-time setup

### 1. NuGet.org account

1. Sign in at [nuget.org](https://www.nuget.org) with a Microsoft account (personal or Entra ID).
2. Pick the NuGet.org **username** on first login — it appears as the package owner — and confirm
   the e-mail address.

### 2. Trusted Publishing policy

On NuGet.org: username → **Trusted Publishing** → **Add**:

| Field | Value |
|---|---|
| Repository owner | `domain-centric-development` |
| Repository | `dca-dotnet` |
| Workflow file | `release.yml` |
| Environment | `nuget` |

One policy covers every package the account owns, so it serves both families and future versions. The
package does not have to exist yet. (A policy for a *new* package id is created in a pending state and
activated by the first successful push — if NuGet.org shows it as pending after the release, nothing is
wrong.)

### 3. GitHub repository

1. **Secret** `NUGET_USER` = the NuGet.org username from step 1 (Settings → Secrets and variables →
   Actions). Not sensitive, but it is the shape `NuGet/login` documents.
2. **Environment** `nuget` (Settings → Environments → New). It is created automatically on the first
   run as well; creating it by hand lets you add *required reviewers*, which puts an approval click in
   front of every push — the NuGet equivalent of the Central Portal's Publish button.

### 4. Package ID prefix

Reserve `DomainCentric` *after* the first package exists: username → **Manage Package ID Prefixes**.
The reservation adds the verified badge; nothing else depends on it.

## Releasing a package family

Versions live in the projects' `<Version>` elements, not on the command line: a `-p:Version=…` handed
to `dotnet pack` is a global property that would also re-version the referenced
`DomainCentric.BuildingBlocks` and turn the packaged dependency into a version that does not exist.
So a release is one commit plus one tag:

1. `<Version>X.Y.Z</Version>` in the family's project(s) — both ArchRules projects for `archrules`.
2. `## [X.Y.Z] - YYYY-MM-DD` section at the top of the family's `CHANGELOG.md`.
3. Commit and push the branch. Then tag:

   ```bash
   git tag building-blocks/v0.1.0
   git push origin building-blocks/v0.1.0
   ```

The workflow refuses to publish unless everything is in order:

1. semantic, non-pre-release version in the tag; every project of the family at `<Version>X.Y.Z</Version>`;
   `## [X.Y.Z]` section present
2. for `archrules`: the `DomainCentric.BuildingBlocks` version it references is already on NuGet.org —
   it becomes the package dependency
3. `dotnet build` and `dotnet test` (Debug — the architecture self-tests need it); rule catalog current
4. `dotnet pack -c Release` per project; every `.nupkg` must contain `LICENSE`, `README.md`, `icon.png`
5. `NuGet/login` (OIDC → short-lived key), `dotnet nuget push --skip-duplicate` per package (the
   `.snupkg` goes along), poll until NuGet.org serves the package, GitHub release with the packages

A pushed version can be **unlisted but never deleted or replaced**. If the environment has required
reviewers, the run pauses before the push until one approves — use that pause to look at the packed
artifacts in the run's summary.

Order for a first release: `building-blocks` first, wait until the run is green and the package is
listed, then `archrules`.

Afterwards: bump `<Version>` in the released projects on `main` and start an `[Unreleased]` section.

## Fallback: publishing from a machine

```bash
security add-generic-password -s dca-nuget -a nuget.org -w    # paste an API key (Push, glob DomainCentric.*)
./scripts/release.sh building-blocks 0.1.0                    # or: archrules 0.1.0
```

The script runs the same preflight (clean tree, free tag, changelog, `<Version>`, dependency on
NuGet.org), builds, tests, packs, inspects the packages, asks for a typed confirmation and pushes. The
API key comes from `NUGET_API_KEY`, the keychain item `dca-nuget`, or a prompt
([scripts/lib/nuget-key.sh](scripts/lib/nuget-key.sh)) — never from a file in the repository or the
shell profile. Tag afterwards; the workflow then finds the packages already on NuGet.org, skips the push
(`--skip-duplicate`) and still cuts the GitHub release. Delete the API key on NuGet.org when done.

## Trying a package before releasing it

```bash
dotnet pack -c Release -o artifacts
dotnet nuget add source "$PWD/artifacts" --name dca-local        # once
cd ../some-consumer && dotnet add package DomainCentric.ArchRules.Xunit --version 0.1.0 --source dca-local
```

`samples/MinimalConsumer` is the smallest such consumer; it builds against the project references here.
