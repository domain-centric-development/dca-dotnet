// Ownership fixture - DCA-TAC-026 passes: an owned aggregate whose repository declares a lookup by its id and the
// owner's id (owner first: the position is free), and an aggregate that belongs to nobody, which needs none.
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Ownership.Good.Records.Domain.Model
{
    public readonly record struct PatientId(string Value) : IId;

    public readonly record struct RecordId(string Value) : IId;

    public readonly record struct WardId(string Value) : IId;

    public sealed class MedicalRecord : AggregateRootBase<MedicalRecord, RecordId>, IOwned<PatientId>
    {
        private MedicalRecord(RecordId id, PatientId patient)
        {
            Id = id;
            Patient = patient;
        }

        public override RecordId Id { get; }

        public PatientId Patient { get; }

        public static MedicalRecord Open(RecordId id, PatientId patient) => new(id, patient);
    }

    /// <summary>Belongs to nobody: its repository needs no scoped lookup.</summary>
    public sealed class Ward : AggregateRootBase<Ward, WardId>
    {
        private Ward(WardId id) => Id = id;

        public override WardId Id { get; }

        public static Ward Open(WardId id) => new(id);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Ownership.Good.Records.Application.Shared
{
    using DomainCentric.ArchRules.Tests.Fixtures.Ownership.Good.Records.Domain.Model;

    public interface IMedicalRecordRepository : IRepository<MedicalRecord, RecordId>
    {
        Task<MedicalRecord?> FindByIdForPatientAsync(PatientId patient, RecordId id, CancellationToken cancellationToken = default);
    }

    public interface IWardRepository : IRepository<Ward, WardId>
    {
    }
}
