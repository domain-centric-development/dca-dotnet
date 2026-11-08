namespace DomainCentric.ArchRules.Tests.Fixtures.Primitives.Good.Ledger.Domain.Model;

public sealed class Account
{
    private bool _frozen;

    public Account(AccountId id) => Id = id;

    public AccountId Id { get; }

    public AccountStatus Status { get; private set; } = AccountStatus.Open;

    public Address? Address { get; private set; }

    public bool Archived { get; private set; }

    /// <summary>Rebuilds a stored account; the stored yes/no fact comes back as the bool it was.</summary>
    public static Account Reconstitute(AccountId id, AccountStatus status, Address? address, bool frozen) =>
        new(id) { Status = status, Address = address, _frozen = frozen };

    public void MoveTo(Address address) => Address = address;

    public void Freeze() => _frozen = true;

    public bool IsFrozen() => _frozen;

    public void Archive() => Archived = true;

    public override bool Equals(object? obj) => obj is Account other && other.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();
}
