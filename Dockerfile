# syntax=docker/dockerfile:1
# Library build: `docker build .` succeeds exactly when the packages build, the tests pass (Debug — ArchUnitNET
# needs the async state machines) and the rule catalog renders. The image carries the .nupkg files and the
# catalog under /out.
#   docker build -t dca-dotnet .
#   docker run --rm dca-dotnet                     → lists the artifacts
#   docker run --rm -v "$PWD/out:/copy" dca-dotnet cp -r /out/. /copy
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /workspace
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
COPY dca-dotnet.sln Directory.Build.props global.json README.md LICENSE ./
COPY branding ./branding
COPY src ./src
COPY tests ./tests
COPY tools ./tools
COPY samples ./samples
RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet build -c Debug --no-restore \
 && dotnet test -c Debug --no-build \
 && dotnet run --project tools/RulesCatalog --no-build -- . \
 && dotnet pack src/DomainCentric.BuildingBlocks -c Release -o /out \
 && dotnet pack src/DomainCentric.ArchRules -c Release -o /out \
 && dotnet pack src/DomainCentric.ArchRules.Xunit -c Release -o /out \
 && cp rules.json RULES.md /out/

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0
COPY --from=build /out /out
CMD ["ls", "-l", "/out"]
