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
/// <para>Every role holds one type name, matched by assignability: a type the project's own type implements or
/// derives from, directly or through an intermediate type. Where a project marks its building blocks with
/// attributes instead, the role takes a <see cref="DcaMarker"/> —
/// <c>WithValue(DcaMarker.OfAttribute("Acme.ValueObjectAttribute"))</c>, or <c>AnyOf</c> a type and an attribute
/// while a migration has both; the attribute counts on the type and on its base types and interfaces, an attribute
/// derived from the named one included. A namespace never names a role. Two roles are recognised by name by default:
/// <c>Command</c> and <c>Query</c>, the input of a use case, <c>OfSuffix("Command")</c> and <c>OfSuffix("Query")</c> —
/// <see cref="WithCommand"/> and <see cref="WithQuery"/> point them at a supertype or an attribute instead. A rule
/// that reads a role's generic arguments or method names (a repository's aggregate, <c>SaveAsync</c>) reads them from
/// the role's type. Every rule asks <see cref="Is(string, ArchUnitNET.Domain.IType)"/>.</para>
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

    /// <summary>Whether every role still names the type of <see cref="Dca"/> and is recognised as by default.</summary>
    public bool IsDefault => Roles().SequenceEqual(Dca().Roles()) && RecognisedOtherwise().Count == 0;

    /// <summary>The two roles that are recognised by name by default: a use case's input.</summary>
    public static readonly IReadOnlyList<string> NamedRoles = new[] { "Command", "Query" };

    private static readonly IReadOnlyDictionary<string, DcaMarker> DefaultRecognition = new Dictionary<string, DcaMarker>(StringComparer.Ordinal)
    {
        ["Command"] = DcaMarker.OfSuffix("Command"),
        ["Query"] = DcaMarker.OfSuffix("Query"),
    };

    private static readonly IReadOnlyList<string> DeclarationRoles = new[] { nameof(BoundedContext), nameof(SharedKernel), nameof(IntegrationEventType) };

    /// <summary>
    /// How a role is recognised where that is more than assignability to its type (WP-160): a building-block role by
    /// <c>OfType</c> and/or <c>OfAttribute</c>, the <c>Command</c> and <c>Query</c> roles also by <c>OfSuffix</c> — their
    /// default — and the three declaration roles by <c>OfAttribute</c>. A role absent from the map is its type, by
    /// assignability. Set through <see cref="WithRole"/>.
    /// </summary>
    public IReadOnlyDictionary<string, DcaMarker> RecognisedBy { get; private init; } = DefaultRecognition;

    /// <summary>
    /// The same vocabulary with one role recognised by the given marker (WP-160). A building-block role takes
    /// <see cref="DcaMarker.OfType"/>, <see cref="DcaMarker.OfAttribute"/> on the type, or both; <c>Command</c> and
    /// <c>Query</c> also <see cref="DcaMarker.OfSuffix"/>; the three declaration roles <c>OfAttribute</c> only. A
    /// namespace never names a role — it says where a type sits, not what it is. At most one <c>OfType</c>: it is the
    /// type the role names, and the rules that read a role's generic arguments or methods read them from it. A role
    /// given without <c>OfType</c> keeps its type name for those readings but is not recognised by assignability to it.
    /// </summary>
    public DcaMarkers WithRole(string role, DcaMarker marker)
    {
        ArgumentNullException.ThrowIfNull(marker);
        var named = NamedRoles.Contains(role);
        var roles = Roles().ToDictionary(r => r.Key, r => r.Value, StringComparer.Ordinal);
        if (!named && !roles.ContainsKey(role))
        {
            throw new ArgumentException(
                $"No marker role named '{role}' - known: {string.Join(", ", roles.Keys.Concat(NamedRoles))}", nameof(role));
        }

        if (DeclarationRoles.Contains(role))
        {
            marker.RequireKinds("The " + role + " role", DcaMarker.Kind.Attribute);
        }
        else if (named)
        {
            marker.RequireKinds("The " + role + " role", DcaMarker.Kind.Type, DcaMarker.Kind.Attribute, DcaMarker.Kind.Suffix);
        }
        else
        {
            marker.RequireKinds("The " + role + " role", DcaMarker.Kind.Type, DcaMarker.Kind.Attribute);
        }

        var types = marker.ValuesOf(DcaMarker.Kind.Type);
        if (types.Count > 1)
        {
            throw new ArgumentException(
                $"The {role} role names one type, was [{string.Join(", ", types)}] - name their common supertype", nameof(marker));
        }

        var next = this;
        if (!named && types.Count == 1)
        {
            next = next.WithRoleType(role, types[0]);
        }

        if (DeclarationRoles.Contains(role))
        {
            next = next.WithRoleType(role, marker.ValuesOf(DcaMarker.Kind.Attribute)[0]);
        }

        var recognition = new Dictionary<string, DcaMarker>(RecognisedBy, StringComparer.Ordinal);
        if ((!named && marker.MarkerKind == DcaMarker.Kind.Type) || DeclarationRoles.Contains(role))
        {
            recognition.Remove(role);
        }
        else
        {
            recognition[role] = marker;
        }

        return next with { RecognisedBy = recognition };
    }

    private DcaMarkers WithRoleType(string role, string type) => role switch
    {
        nameof(AggregateRoot) => this with { AggregateRoot = type },
        nameof(Entity) => this with { Entity = type },
        nameof(Value) => this with { Value = type },
        nameof(Id) => this with { Id = type },
        nameof(DomainEvent) => this with { DomainEvent = type },
        nameof(IntegrationEvent) => this with { IntegrationEvent = type },
        nameof(DomainService) => this with { DomainService = type },
        nameof(Factory) => this with { Factory = type },
        nameof(Specification) => this with { Specification = type },
        nameof(DomainException) => this with { DomainException = type },
        nameof(UseCaseException) => this with { UseCaseException = type },
        nameof(TransactionBoundary) => this with { TransactionBoundary = type },
        nameof(InputPort) => this with { InputPort = type },
        nameof(UseCase) => this with { UseCase = type },
        nameof(OutputPort) => this with { OutputPort = type },
        nameof(Repository) => this with { Repository = type },
        nameof(Store) => this with { Store = type },
        nameof(DomainEventPublisher) => this with { DomainEventPublisher = type },
        nameof(IntegrationEventPublisher) => this with { IntegrationEventPublisher = type },
        nameof(BoundedContext) => this with { BoundedContext = type },
        nameof(SharedKernel) => this with { SharedKernel = type },
        nameof(IntegrationEventType) => this with { IntegrationEventType = type },
        _ => throw new ArgumentException($"No marker role named '{role}'", nameof(role)),
    };

    /// <summary>How a role is recognised: its marker, or <c>OfType</c> of the type it names.</summary>
    public DcaMarker Marker(string role)
    {
        if (RecognisedBy.TryGetValue(role, out var marker))
        {
            return marker;
        }

        var type = Roles().FirstOrDefault(r => r.Key == role).Value
            ?? throw new ArgumentException($"No marker role named '{role}'", nameof(role));
        return DeclarationRoles.Contains(role) ? DcaMarker.OfAttribute(type) : DcaMarker.OfType(type);
    }

    /// <summary>The roles recognised otherwise than by plain assignability, for the report.</summary>
    public IReadOnlyList<KeyValuePair<string, DcaMarker>> RecognisedOtherwise() =>
        RecognisedBy
            .Where(r => !(DefaultRecognition.TryGetValue(r.Key, out var byDefault) && byDefault.Equals(r.Value)))
            .OrderBy(r => RoleOrder(r.Key))
            .ToArray();

    private static int RoleOrder(string role)
    {
        var index = Dca().Roles().Select(r => r.Key).ToList().IndexOf(role);
        return index >= 0 ? index : 100 + NamedRoles.ToList().IndexOf(role);
    }

    /// <summary>
    /// Whether the imported type is one the role recognises: assignable to its type, carrying its attribute — on the
    /// type or one of its base types or interfaces, an attribute derived from the named one included — or named with
    /// its suffix, as its marker says. The one predicate every rule asks for a role.
    /// </summary>
    public bool Is(string role, ArchUnitNET.Domain.IType type) => Matches(Marker(role), type);

    /// <summary><see cref="Is(string, ArchUnitNET.Domain.IType)"/> for a reflected type.</summary>
    public bool Is(string role, Type? type) => type is not null && Matches(Marker(role), type);

    /// <summary>Whether the imported type is one the marker recognises.</summary>
    public static bool Matches(DcaMarker marker, ArchUnitNET.Domain.IType type)
    {
        // A generic parameter stands for any type; ArchUnitNET answers assignability for it with yes.
        if (type.IsGenericParameter)
        {
            return false;
        }

        foreach (var alternative in marker.Alternatives)
        {
            var name = alternative.Value!;
            var hit = alternative.MarkerKind switch
            {
                DcaMarker.Kind.Type => type.FullName == name || ArchUnitNET.Domain.Extensions.TypeExtensions.IsAssignableTo(type, name),
                DcaMarker.Kind.Attribute => AttributedInHierarchy(type, name),
                DcaMarker.Kind.Suffix => SimpleName(type.Name).EndsWith(name, StringComparison.Ordinal),
                _ => false,
            };
            if (hit)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Whether the reflected type is one the marker recognises.</summary>
    public static bool Matches(DcaMarker marker, Type type)
    {
        if (type.IsGenericParameter)
        {
            return false;
        }

        foreach (var alternative in marker.Alternatives)
        {
            var name = alternative.Value!;
            var hit = alternative.MarkerKind switch
            {
                DcaMarker.Kind.Type => IsAssignableToByName(type, name) || CarriesRole(type, name),
                DcaMarker.Kind.Attribute => AttributedInHierarchy(type, name),
                DcaMarker.Kind.Suffix => SimpleName(type.Name).EndsWith(name, StringComparison.Ordinal),
                _ => false,
            };
            if (hit)
            {
                return true;
            }
        }

        return false;
    }

    private static string SimpleName(string name) => name.Split('`')[0];

    private static bool AttributedInHierarchy(ArchUnitNET.Domain.IType type, string attribute)
    {
        var hierarchy = new List<ArchUnitNET.Domain.IType> { type };
        for (var current = (type as ArchUnitNET.Domain.Class)?.BaseClass; current is not null; current = current.BaseClass)
        {
            hierarchy.Add(current);
        }

        hierarchy.AddRange(type.ImplementedInterfaces);
        return hierarchy.Any(t => t.Attributes.Any(a =>
            a.FullName == attribute || ArchUnitNET.Domain.Extensions.TypeExtensions.IsAssignableTo(a, attribute)));
    }

    private static bool AttributedInHierarchy(Type type, string attribute)
    {
        try
        {
            var hierarchy = new List<Type>();
            for (var current = type; current is not null; current = current.BaseType)
            {
                hierarchy.Add(current);
            }

            hierarchy.AddRange(type.GetInterfaces());
            return hierarchy.Any(t => t.GetCustomAttributesData().Any(a => IsAssignableToByName(a.AttributeType, attribute)));
        }
        catch (Exception e) when (e is TypeLoadException or System.IO.FileNotFoundException or System.IO.FileLoadException)
        {
            return false;
        }
    }

    public DcaMarkers WithAggregateRoot(DcaMarker marker) => WithRole(nameof(AggregateRoot), marker);

    public DcaMarkers WithEntity(DcaMarker marker) => WithRole(nameof(Entity), marker);

    public DcaMarkers WithValue(DcaMarker marker) => WithRole(nameof(Value), marker);

    public DcaMarkers WithId(DcaMarker marker) => WithRole(nameof(Id), marker);

    public DcaMarkers WithDomainEvent(DcaMarker marker) => WithRole(nameof(DomainEvent), marker);

    public DcaMarkers WithIntegrationEvent(DcaMarker marker) => WithRole(nameof(IntegrationEvent), marker);

    public DcaMarkers WithDomainService(DcaMarker marker) => WithRole(nameof(DomainService), marker);

    public DcaMarkers WithFactory(DcaMarker marker) => WithRole(nameof(Factory), marker);

    public DcaMarkers WithSpecification(DcaMarker marker) => WithRole(nameof(Specification), marker);

    public DcaMarkers WithDomainException(DcaMarker marker) => WithRole(nameof(DomainException), marker);

    public DcaMarkers WithUseCaseException(DcaMarker marker) => WithRole(nameof(UseCaseException), marker);

    public DcaMarkers WithTransactionBoundary(DcaMarker marker) => WithRole(nameof(TransactionBoundary), marker);

    public DcaMarkers WithInputPort(DcaMarker marker) => WithRole(nameof(InputPort), marker);

    public DcaMarkers WithUseCase(DcaMarker marker) => WithRole(nameof(UseCase), marker);

    public DcaMarkers WithOutputPort(DcaMarker marker) => WithRole(nameof(OutputPort), marker);

    public DcaMarkers WithRepository(DcaMarker marker) => WithRole(nameof(Repository), marker);

    public DcaMarkers WithStore(DcaMarker marker) => WithRole(nameof(Store), marker);

    public DcaMarkers WithDomainEventPublisher(DcaMarker marker) => WithRole(nameof(DomainEventPublisher), marker);

    public DcaMarkers WithIntegrationEventPublisher(DcaMarker marker) => WithRole(nameof(IntegrationEventPublisher), marker);

    public DcaMarkers WithBoundedContext(DcaMarker marker) => WithRole(nameof(BoundedContext), marker);

    public DcaMarkers WithSharedKernel(DcaMarker marker) => WithRole(nameof(SharedKernel), marker);

    public DcaMarkers WithIntegrationEventType(DcaMarker marker) => WithRole(nameof(IntegrationEventType), marker);

    /// <summary>How a use case's input is recognised as a command — <c>OfSuffix("Command")</c> by default.</summary>
    public DcaMarkers WithCommand(DcaMarker marker) => WithRole("Command", marker);

    /// <summary>How a use case's input is recognised as a query — <c>OfSuffix("Query")</c> by default.</summary>
    public DcaMarkers WithQuery(DcaMarker marker) => WithRole("Query", marker);

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
