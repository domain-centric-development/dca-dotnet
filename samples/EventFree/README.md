# EventFree

Production dependency: DomainCentric.BuildingBlocks only, via the sibling project. Test dependencies: DomainCentric.ArchRules and xUnit. Run `dotnet test ArchitectureTests -c Debug --logger "console;verbosity=detailed"`. The test selects usecase/onion/hexagonal/naming/advanced/cycle/tactical/dotnet and prints the actual count; context-map/deployment topology are outside this focused consumer. There is no declarative-transaction twin because that variant is Java-only.
