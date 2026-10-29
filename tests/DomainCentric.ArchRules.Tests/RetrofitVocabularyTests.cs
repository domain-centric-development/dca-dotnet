using System;
using System.Linq;
using Xunit;

namespace DomainCentric.ArchRules.Tests;

/// <summary>
/// A project retrofitted with its own vocabulary: its own strategic and integration-event attributes, its own
/// input-port naming, a host named like a layer, an adapter-internal store abstraction, a closed domain outcome,
/// an enum result and a local output port. Each convention is configured, none is switched off.
/// </summary>
public sealed class RetrofitVocabularyTests
{
    private const string App = "DomainCentric.ArchRules.Tests.Fixtures.Retrofit";
    private const string Vocabulary = App + ".Vocabulary.";
    private const string Open = "DomainCentric.ArchRules.Tests.Fixtures.RetrofitOpen";

    private static DcaMarkers OwnMarkers() => DcaMarkers.Dca() with
    {
        Name = "retrofit",
        BoundedContext = Vocabulary + "BoundedContextAttribute",
        SharedKernel = Vocabulary + "SharedKernelAttribute",
        IntegrationEventType = Vocabulary + "IntegrationEventTypeAttribute",
        IntegrationEvent = Vocabulary + "IIntegrationEvent",
        InputPort = Vocabulary + "IInputPort",
        UseCase = Vocabulary + "IInputPort",
        OutputPort = Vocabulary + "IOutputPort",
        Value = Vocabulary + "IValue",
    };

    private static DcaLayout Configured(string root = App) =>
        DcaLayout.ForRootNamespace(root)
            .WithExcludedNamespaces(root + ".Application")
            .WithUseCaseSuffix("ApplicationService")
            .WithInputPortSuffix("UseCase")
            .WithLocalOutputPorts(Vocabulary + "ILocalPort")
            .WithFrameworkTypes(FrameworkTypes.AspNetCore() with { TransactionalAttribute = Vocabulary + "UnitOfWorkAttribute" })
            .WithMarkers(OwnMarkers());

    /// <summary>The configured vocabulary without the excluded host, which only <see cref="App"/> has.</summary>
    private static DcaLayout ConfiguredOpen() =>
        DcaLayout.ForRootNamespace(Open).WithMarkers(OwnMarkers());

    private static DcaArchitecture Arch(DcaLayout layout) =>
        DcaArchitecture.Load(layout, typeof(RetrofitVocabularyTests).Assembly);

    private static IDcaRule Rule(DcaLayout layout, string id) => DcaRules.All(layout).Single(r => r.Id == id);

    private static void Passes(DcaLayout layout, string id) => Rule(layout, id).Check(Arch(layout));

    private static string Fails(DcaLayout layout, string id) =>
        Assert.Throws<DcaRuleViolationException>(() => Rule(layout, id).Check(Arch(layout))).Message;

    [Fact]
    public void TheOwnContextAttributeIsReadByName()
    {
        var arch = Arch(Configured());

        Assert.Equal(new[] { App + ".Billing" }, arch.BoundedContexts.Keys.ToArray());
        Assert.Equal("Billing", arch.BoundedContexts[App + ".Billing"].Name);
        Assert.Equal("Invoices and their payment", arch.BoundedContexts[App + ".Billing"].Description);
    }

    [Fact]
    public void TheOwnSharedKernelAttributeDeclaresTheKernel()
    {
        Assert.Equal(App + ".Common", Arch(Configured()).SharedKernelNamespace);
    }

    [Fact]
    public void WithoutTheRolesTheOwnAttributesAreNotSeen()
    {
        var arch = Arch(DcaLayout.ForRootNamespace(App));

        Assert.Empty(arch.BoundedContexts);
        Assert.Null(arch.SharedKernelNamespace);
    }

    [Fact]
    public void AHostNamedLikeALayerIsNamedAsTheCause()
    {
        var layout = DcaLayout.ForRootNamespace(App).WithMarkers(OwnMarkers());
        var arch = Arch(layout);

        Assert.Contains(App, arch.ModuleRoots());
        Fails(layout, "DCA-HEX-007");
        Assert.Contains(App + ".Application", arch.RootModuleRootNotice());
    }

    [Fact]
    public void TheExcludedHostLeavesTheContextsApart()
    {
        var arch = Arch(Configured());

        Assert.DoesNotContain(App, arch.ModuleRoots());
        Assert.Null(arch.RootModuleRootNotice());
        Assert.DoesNotContain(arch.Types, t => t.FullName == App + ".Application.Host");
        Assert.DoesNotContain(arch.Architecture.Types, t => t.FullName == App + ".Application.Host");
        Passes(Configured(), "DCA-HEX-007");
        Passes(Configured(), "DCA-HEX-002");
    }

    [Fact]
    public void TheRootNamespaceCannotBeExcluded()
    {
        Assert.Throws<ArgumentException>(() => DcaLayout.ForRootNamespace(App).WithExcludedNamespaces(App));
    }

    [Fact]
    public void TheInputPortSuffixIsConfigurable()
    {
        var message = Fails(DcaLayout.ForRootNamespace(App).WithMarkers(OwnMarkers()), "DCA-NAM-003");

        Assert.Contains("IPayInvoiceUseCase", message);
        Passes(Configured(), "DCA-NAM-003");
        Passes(Configured(), "DCA-NAM-001");
    }

    [Fact]
    public void TheOwnInputPortIsNotARedeclaration()
    {
        Assert.Contains("IInputPort", Fails(DcaLayout.ForRootNamespace(App), "DCA-USE-001"));
        Passes(Configured(), "DCA-USE-001");
    }

    [Fact]
    public void TheOwnIntegrationEventTypeCounts()
    {
        var withoutRole = Configured().WithMarkers(OwnMarkers() with { IntegrationEventType = "X.Y.MissingAttribute" });

        Assert.Contains("InvoicePaid", Fails(withoutRole, "DCA-ADV-005"));
        Passes(Configured(), "DCA-ADV-005");
    }

    [Fact]
    public void AnAdaptersOwnStoreAbstractionIsNotAPort()
    {
        Passes(Configured(), "DCA-TAC-018");
        Assert.Contains("IReceiptStore", Fails(ConfiguredOpen(), "DCA-TAC-018"));
    }

    [Fact]
    public void ClosedDomainOutcomesPassAndOpenOnesDoNot()
    {
        Passes(Configured(), "DCA-USE-006");

        var message = Fails(ConfiguredOpen(), "DCA-USE-006");
        Assert.Contains("RefundResult", message);
    }

    [Fact]
    public void AnEnumResultIsNotAShapeFinding()
    {
        Passes(Configured(), "DCA-USE-007");
    }

    [Fact]
    public void ALocalOutputPortIsNotRemote()
    {
        var withoutLocal = Configured().WithLocalOutputPorts();

        Assert.Contains("ISecretHasher", Fails(withoutLocal, "DCA-USE-013"));
        Passes(Configured(), "DCA-USE-013");
    }

    [Fact]
    public void TheConventionalKernelNamespaceIsTheKernelWhenNothingIsDeclared()
    {
        const string root = "DomainCentric.ArchRules.Tests.Fixtures.RetrofitKernel";

        Assert.Equal(root + ".SharedKernel", Arch(DcaLayout.ForRootNamespace(root)).SharedKernelNamespace);
    }

    [Fact]
    public void AMissingMemberIsNamed()
    {
        const string root = "DomainCentric.ArchRules.Tests.Fixtures.RetrofitNoName";
        var arch = Arch(DcaLayout.ForRootNamespace(root)
            .WithMarkers(DcaMarkers.Dca() with { BoundedContext = root + ".ContextLabelAttribute" }));

        var error = Assert.Throws<InvalidOperationException>(() => arch.BoundedContexts.Count);

        Assert.Contains("ContextLabelAttribute", error.Message);
        Assert.Contains(root + ".Ordering", error.Message);
        Assert.Contains("Name", error.Message);
    }
}
