using System;
using System.Collections.Generic;
using System.Linq;

namespace DomainCentric.ArchRules;

/// <summary>
/// The building-block types the DCA rules select on, by <em>role</em>, each named by its full type
/// name. A rule asks the layout for the role it needs — "the aggregate-root marker", "the repository
/// port" — and never for a specific type, so the same rules govern a code base that carries its own
/// markers or another library's.
/// </summary>
/// <remarks>
/// <para>The default is <see cref="Dca"/>: the markers of <c>DomainCentric.BuildingBlocks</c>. A project
/// that already has an established vocabulary keeps it and points the roles at its own types with a
/// <c>with</c> expression:
/// <c>DcaLayout.ForRootNamespace("Acme.Billing").WithMarkers(DcaMarkers.Dca() with { Name = "own",
/// AggregateRoot = "Acme.Platform.IContractRoot", Repository = "Acme.Platform.IContractStore" })</c>.</para>
/// <para>Every role holds exactly one type name, matched by assignability: a type the project's own type
/// implements or derives from, directly or through an intermediate type. Two vocabularies at once — this
/// library's marker on one aggregate and another's on the next — are outside this: a role names one type,
/// so a migration points the role at one of them and the rules select what carries it.</para>
/// <para><b>The port roles need a common root.</b> <c>OutputPort</c> must be set whenever any other port
/// role is: <c>DCA-HEX-009</c> measures every output port of the project against it, so leaving it at the
/// library's own type while <c>Repository</c> points elsewhere reports every port of the project's
/// vocabulary. A vocabulary that has no common port supertype cannot express that here — switch
/// <c>DCA-HEX-009</c> off rather than pointing the role at an unrelated type.</para>
/// <para><b>Three roles name attributes</b>, not supertypes: <c>BoundedContext</c> and <c>SharedKernel</c> on a
/// context marker class, <c>IntegrationEventType</c> on an integration event. They are matched by full type
/// name, never by a type literal, and the members the rules read are read by name: a project's own
/// <c>[BoundedContext(name, Description = …)]</c> serves like the library's. A member the rules need and the
/// attribute lacks is reported by name.</para>
/// <para>What the roles do <em>not</em> cover are the relationship attributes (<c>[Upstream]</c>,
/// <c>[ExternalUpstream]</c>, <c>[Partnership]</c>, <c>[OpenHostService]</c>). The rules read their nested values
/// and enum members — the channels a relationship consumes, its translation and its status — and those stay
/// the library's own.</para>
/// <para><b>A role names a type, never a method.</b> Four method names are fixed, and pointing a role at
/// your own type does not rename them: <c>SaveAsync</c> and <c>DeleteByIdAsync</c> on the repository role,
/// <c>PublishAndClearEventsAsync</c> on the domain-event-publisher role, and <c>RegisterEvent</c> on the
/// aggregate root. The rules that reason about a unit of work — <c>DCA-USE-009</c>, <c>DCA-USE-012</c> and
/// <c>DCA-TAC-021</c> — look for calls by those names. A vocabulary whose repository writes under another
/// name is selected correctly and then found to save nothing, so those rules pass over it. Keep the names,
/// or switch the affected ids off with a recorded reason.</para>
/// </remarks>
/// <param name="Name">The vocabulary's name (<c>dca</c>, or whatever a with-expression set). It appears in
/// <see cref="DcaLayout.MarkersReport"/> and in <see cref="DcaLayout.ToString"/>, together with the roles that
/// differ from the default, so a reader can see which types the rules resolved. The xUnit base class has no
/// diagnostic case of its own, unlike the Java library's, which prints the line as a passing test.</param>
public sealed record DcaMarkers(string Name)
{
    /// <summary>The namespace the default vocabulary lives in.</summary>
    public const string DcaBuildingBlocks = "DomainCentric.BuildingBlocks";

    /// <summary>The marker of an aggregate root.</summary>
    public string AggregateRoot { get; init; } = DcaBuildingBlocks + ".Ddd.Tactical.IAggregateRoot";

    /// <summary>The marker of an entity.</summary>
    public string Entity { get; init; } = DcaBuildingBlocks + ".Ddd.Tactical.IEntity";

    /// <summary>The marker of a value object.</summary>
    public string Value { get; init; } = DcaBuildingBlocks + ".Ddd.Tactical.IValue";

    /// <summary>The marker of an identifier.</summary>
    public string Id { get; init; } = DcaBuildingBlocks + ".Ddd.Tactical.IId";

    /// <summary>The marker of a domain event.</summary>
    public string DomainEvent { get; init; } = DcaBuildingBlocks + ".Ddd.Tactical.IDomainEvent";

    /// <summary>The marker of an integration event.</summary>
    public string IntegrationEvent { get; init; } = DcaBuildingBlocks + ".Ddd.Tactical.IIntegrationEvent";

    /// <summary>The marker of a domain service.</summary>
    public string DomainService { get; init; } = DcaBuildingBlocks + ".Ddd.Tactical.IDomainService";

    /// <summary>The marker of a factory.</summary>
    public string Factory { get; init; } = DcaBuildingBlocks + ".Ddd.Tactical.IFactory";

    /// <summary>
    /// The marker of a specification — a predicate over a domain object. <c>DCA-ADV-017</c> and
    /// <c>DCA-ADV-018</c> select through it alongside the name suffix, because a project may carry the
    /// marker without naming the type <c>*Specification</c>.
    /// </summary>
    public string Specification { get; init; } = DcaBuildingBlocks + ".Ddd.Tactical.ISpecification";

    /// <summary>The base type of a domain failure.</summary>
    public string DomainException { get; init; } = DcaBuildingBlocks + ".Ddd.Tactical.DomainException";

    /// <summary>The base type of a use-case failure.</summary>
    public string UseCaseException { get; init; } = DcaBuildingBlocks + ".Application.UseCaseException";

    /// <summary>The explicit transaction boundary of the application layer.</summary>
    public string TransactionBoundary { get; init; } = DcaBuildingBlocks + ".Application.Transactions.ITransactionBoundary";

    /// <summary>The base type of every incoming (driving) port.</summary>
    public string InputPort { get; init; } = DcaBuildingBlocks + ".Hexagonal.Ports.In.IInputPort";

    /// <summary>
    /// The input port that takes a command or query and answers with a result. Read by <c>DCA-NAM-001</c>, by
    /// <c>DCA-NET-003</c>, which checks the contract's signature, and where a rule looks for an operation's
    /// input type; every other use-case rule selects through <see cref="InputPort"/> or the use-case suffix. A
    /// vocabulary with several use-case shapes (one that answers, one that answers nothing) points this role at
    /// their common supertype, which may be the input port; the input type is then taken from the parameters of
    /// the input port's methods.
    /// </summary>
    public string UseCase { get; init; } = DcaBuildingBlocks + ".Hexagonal.Ports.In.IUseCase";

    /// <summary>The base type of every outgoing (driven) port.</summary>
    public string OutputPort { get; init; } = DcaBuildingBlocks + ".Hexagonal.Ports.Out.IOutputPort";

    /// <summary>The outgoing port that loads and stores an aggregate.</summary>
    public string Repository { get; init; } = DcaBuildingBlocks + ".Hexagonal.Ports.Out.IRepository";

    /// <summary>The outgoing port that holds data no aggregate owns.</summary>
    public string Store { get; init; } = DcaBuildingBlocks + ".Hexagonal.Ports.Out.IStore";

    /// <summary>The outgoing port that publishes an aggregate's domain events.</summary>
    public string DomainEventPublisher { get; init; } = DcaBuildingBlocks + ".Hexagonal.Ports.Out.IDomainEventPublisher";

    /// <summary>
    /// The outgoing port that publishes an integration event. Read by <c>DCA-USE-013</c>, which separates the
    /// output ports that live inside the transaction from those that may leave the process.
    /// </summary>
    public string IntegrationEventPublisher { get; init; } = DcaBuildingBlocks + ".Hexagonal.Ports.Out.IIntegrationEventPublisher";

    /// <summary>
    /// The attribute that declares a bounded context on a context marker class. The rules read its members
    /// <c>Name</c> and <c>Description</c> by name, as public properties, so the project's own attribute must
    /// carry <c>Name</c>; <c>Description</c> may be absent.
    /// </summary>
    public string BoundedContext { get; init; } = DcaBuildingBlocks + ".Ddd.Strategic.BoundedContextAttribute";

    /// <summary>
    /// The attribute that declares the shared kernel on a context marker class. Only its presence is read; a
    /// member <c>Description</c> is read when the attribute has one.
    /// </summary>
    public string SharedKernel { get; init; } = DcaBuildingBlocks + ".Ddd.Strategic.Relationships.SharedKernelAttribute";

    /// <summary>
    /// The attribute that gives an integration event its contract identity. <c>DCA-ADV-005</c> reads only its
    /// presence on the event type.
    /// </summary>
    public string IntegrationEventType { get; init; } = DcaBuildingBlocks + ".Ddd.Tactical.IntegrationEventTypeAttribute";

    /// <summary>The markers of <c>DomainCentric.BuildingBlocks</c> — the default of <see cref="DcaLayout"/>.</summary>
    public static DcaMarkers Dca() => new("dca");

    /// <summary>Whether every role still names the type of <see cref="Dca"/>.</summary>
    public bool IsDefault => Roles().SequenceEqual(Dca().Roles());

    /// <summary>
    /// Refuses a vocabulary that cannot be used: a blank name, or a role that names no type. A role
    /// selects by assignability, so an empty one would silently select nothing and report success —
    /// the failure mode these rules exist against. <see cref="DcaLayout"/> calls this when it is built.
    /// </summary>
    public DcaMarkers Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("Name must not be blank", nameof(Name));
        }

        Roles();
        return this;
    }

    /// <summary>The roles by name, in declaration order — what a report prints when they are not default.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Roles()
    {
        var roles = new[]
        {
            new KeyValuePair<string, string>(nameof(AggregateRoot), AggregateRoot),
            new KeyValuePair<string, string>(nameof(Entity), Entity),
            new KeyValuePair<string, string>(nameof(Value), Value),
            new KeyValuePair<string, string>(nameof(Id), Id),
            new KeyValuePair<string, string>(nameof(DomainEvent), DomainEvent),
            new KeyValuePair<string, string>(nameof(IntegrationEvent), IntegrationEvent),
            new KeyValuePair<string, string>(nameof(DomainService), DomainService),
            new KeyValuePair<string, string>(nameof(Factory), Factory),
            new KeyValuePair<string, string>(nameof(Specification), Specification),
            new KeyValuePair<string, string>(nameof(DomainException), DomainException),
            new KeyValuePair<string, string>(nameof(UseCaseException), UseCaseException),
            new KeyValuePair<string, string>(nameof(TransactionBoundary), TransactionBoundary),
            new KeyValuePair<string, string>(nameof(InputPort), InputPort),
            new KeyValuePair<string, string>(nameof(UseCase), UseCase),
            new KeyValuePair<string, string>(nameof(OutputPort), OutputPort),
            new KeyValuePair<string, string>(nameof(Repository), Repository),
            new KeyValuePair<string, string>(nameof(Store), Store),
            new KeyValuePair<string, string>(nameof(DomainEventPublisher), DomainEventPublisher),
            new KeyValuePair<string, string>(nameof(IntegrationEventPublisher), IntegrationEventPublisher),
            new KeyValuePair<string, string>(nameof(BoundedContext), BoundedContext),
            new KeyValuePair<string, string>(nameof(SharedKernel), SharedKernel),
            new KeyValuePair<string, string>(nameof(IntegrationEventType), IntegrationEventType),
        };
        foreach (var role in roles)
        {
            if (string.IsNullOrWhiteSpace(role.Value))
            {
                throw new ArgumentException(
                    $"{role.Key} must name a type: a role selects by assignability, and an empty role would silently select nothing",
                    role.Key);
            }
        }

        return roles;
    }

    /// <summary>
    /// Whether a reflected type is the named one or carries it, walking interfaces and base types. A role
    /// names a type rather than a <see cref="Type"/>, so assignability is asked by name.
    /// </summary>
    public static bool IsAssignableToByName(Type? type, string fullName)
    {
        if (type is null)
        {
            return false;
        }

        if (type.FullName == fullName)
        {
            return true;
        }

        return type.GetInterfaces().Any(i => i.FullName == fullName)
            || IsAssignableToByName(type.BaseType, fullName);
    }

    /// <summary>
    /// The namespaces the role types themselves live in — the vocabulary's own code, which is never the
    /// project's. Rules that ask "is this the project's own type or the vocabulary's" use this instead of a
    /// hard-wired library namespace: with the default roles it yields the building blocks' namespaces, with a
    /// project's own markers the namespaces those markers live in. Deriving it from the roles is what keeps
    /// <c>DCA-ERR-002</c> from reporting a foreign base type as an exception outside its layer.
    /// </summary>
    public IReadOnlyList<string> DeclaringNamespaces() =>
        NamespacesOf(Roles().Select(r => r.Value));

    /// <summary>
    /// The roles whose types a domain type may legitimately depend on: the tactical vocabulary and the
    /// outgoing ports. The application-layer roles (<c>UseCaseException</c>, <c>TransactionBoundary</c>)
    /// and the incoming ports (<c>InputPort</c>, <c>UseCase</c>) are deliberately absent — a domain type
    /// reaching for one of them is the violation <c>DCA-ONI-002</c> exists to report.
    /// </summary>
    public static readonly IReadOnlyList<string> DomainFacingRoles = new[]
    {
        nameof(AggregateRoot),
        nameof(Entity),
        nameof(Value),
        nameof(Id),
        nameof(DomainEvent),
        nameof(IntegrationEvent),
        nameof(DomainService),
        nameof(Factory),
        nameof(Specification),
        nameof(DomainException),
        nameof(OutputPort),
        nameof(Repository),
        nameof(Store),
        nameof(DomainEventPublisher),
        nameof(IntegrationEventPublisher),
    };

    /// <summary>
    /// The namespaces of the named roles.
    /// </summary>
    /// <remarks>
    /// This is what a rule asks for when it needs "the namespaces the vocabulary declares these types in"
    /// rather than a hard-wired library namespace. With the default roles and <see cref="DomainFacingRoles"/>
    /// it yields the building blocks' tactical and outgoing-port namespaces — exactly the two that used to be
    /// written into <c>DCA-ONI-002</c> by hand — and with a project's own markers it yields the namespaces
    /// those markers live in, so the project's vocabulary is not reported as a foreign library inside its own
    /// domain. An unknown role name is ignored.
    /// </remarks>
    public IReadOnlyList<string> DeclaringNamespacesOf(IEnumerable<string> roleNames)
    {
        var roles = Roles().ToDictionary(r => r.Key, r => r.Value, StringComparer.Ordinal);
        return NamespacesOf(roleNames.Select(name => roles.TryGetValue(name, out var fqn) ? fqn : null)
            .Where(fqn => fqn is not null)!);
    }

    private static IReadOnlyList<string> NamespacesOf(IEnumerable<string> fullyQualifiedNames) =>
        fullyQualifiedNames
            .Select(fqn => fqn.Contains('.') ? fqn[..fqn.LastIndexOf('.')] : "")
            .Where(ns => ns.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(ns => ns, StringComparer.Ordinal)
            .ToArray();

    /// <summary>Whether the named namespace is one the vocabulary's own types live in, or one below it.</summary>
    public bool DeclaresTypesIn(string namespaceName) =>
        DeclaringNamespaces().Any(ns => DcaLayout.IsBelow(namespaceName, ns));

    /// <summary>
    /// Whether a type is the role's own type, with or without a generic arity suffix: a role names the
    /// non-generic marker (<c>IUseCase</c>), while a reflected generic definition carries the arity
    /// (<c>IUseCase`2</c>).
    /// </summary>
    public static bool IsRole(Type? type, string role)
    {
        if (type?.FullName is not { } name)
        {
            return false;
        }

        return name == role || name.StartsWith(role + "`", StringComparison.Ordinal);
    }

    /// <summary>
    /// Whether a reflected type carries the role through one of its interfaces or is it — generic
    /// instantiations are compared through their definition, so <c>IUseCase&lt;TCommand, TResult&gt;</c>
    /// matches the <c>UseCase</c> role.
    /// </summary>
    public static bool CarriesRole(Type? type, string role)
    {
        if (type is null)
        {
            return false;
        }

        if (IsRole(type, role) || (type.IsGenericType && IsRole(type.GetGenericTypeDefinition(), role)))
        {
            return true;
        }

        return type.GetInterfaces().Any(
            i => IsRole(i, role) || (i.IsGenericType && IsRole(i.GetGenericTypeDefinition(), role)));
    }

    public override string ToString() => Name;
}
