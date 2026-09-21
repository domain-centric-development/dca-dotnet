// A project that points the use-case role at a contract of its own. DCA-NET-003 then has something to
// say: with the library's own IUseCase<TIn,TOut> the C# compiler already enforces the signature.
using DomainCentric.BuildingBlocks.Ddd.Strategic;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.In;

namespace DomainCentric.ArchRules.Tests.Fixtures.Dotnet.OwnContract.Vocabulary
{
    /// <summary>A use-case contract that is neither awaitable nor cancellable.</summary>
    public interface IHandler<in TInput, out TOutput> : IInputPort
    {
        TOutput Handle(TInput input);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Dotnet.OwnContract.Parcel
{
    [BoundedContext("Parcel", Description = "own use-case contract")]
    public static class ParcelContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Dotnet.OwnContract.Parcel.Application.ShipParcel
{
    using DomainCentric.ArchRules.Tests.Fixtures.Dotnet.OwnContract.Vocabulary;

    public sealed record ShipParcelCommand(string TrackingCode);

    public sealed record ShipParcelResult(string TrackingCode);

    public interface IShipParcelInputPort : IHandler<ShipParcelCommand, ShipParcelResult>
    {
    }

    // DCA-NET-003: synchronous, no CancellationToken.
    public sealed class ShipParcelUseCase : IShipParcelInputPort
    {
        public ShipParcelResult Handle(ShipParcelCommand input) => new(input.TrackingCode);
    }
}
