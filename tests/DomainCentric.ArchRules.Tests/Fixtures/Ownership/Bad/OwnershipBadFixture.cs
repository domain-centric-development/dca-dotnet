// Ownership fixture - DCA-TAC-026 fails: an owned aggregate whose repository loads it by id alone (the question by
// owner answers yes or no and does not count), and an aggregate that implements only the non-generic marker.
using System.Threading;
using System.Threading.Tasks;
using DomainCentric.BuildingBlocks.Ddd.Tactical;
using DomainCentric.BuildingBlocks.Hexagonal.Ports.Out;

namespace DomainCentric.ArchRules.Tests.Fixtures.Ownership.Bad.Records.Domain.Model
{
    public readonly record struct PatientId(string Value) : IId;

    public readonly record struct RecordId(string Value) : IId;

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

    /// <summary>Owned, but names no owner id type.</summary>
    public sealed class Consent : AggregateRootBase<Consent, RecordId>, IOwned
    {
        private Consent(RecordId id) => Id = id;

        public override RecordId Id { get; }

        public static Consent Give(RecordId id) => new(id);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Ownership.Bad.Records.Application.Shared
{
    using DomainCentric.ArchRules.Tests.Fixtures.Ownership.Bad.Records.Domain.Model;

    public interface IMedicalRecordRepository : IRepository<MedicalRecord, RecordId>
    {
        Task<bool> ExistsByIdForPatientAsync(RecordId id, PatientId patient, CancellationToken cancellationToken = default);
    }
}
