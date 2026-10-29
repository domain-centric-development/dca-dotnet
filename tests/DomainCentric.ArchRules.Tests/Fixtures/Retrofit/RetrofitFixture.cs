// Retrofit fixture — a project with its own vocabulary: its own context, shared-kernel and integration-event
// attributes, its own input-port naming, a host named like a layer, an adapter-internal store abstraction, a
// closed domain outcome, an enum result and a local output port. Each convention is configured, none is
// switched off. RetrofitOpen holds the counterparts the configuration must still report.
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DomainCentric.ArchRules.Tests.Fixtures.Retrofit.Vocabulary
{
    /// <summary>A project's own context declaration.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class BoundedContextAttribute : Attribute
    {
        public BoundedContextAttribute(string name) => Name = name;

        public string Name { get; }

        public string Description { get; set; } = "";
    }

    /// <summary>A project's own shared-kernel declaration, without members.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SharedKernelAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class IntegrationEventTypeAttribute : Attribute
    {
        public IntegrationEventTypeAttribute(string name) => Name = name;

        public string Name { get; }

        public int Version { get; set; } = 1;
    }

    /// <summary>The project's own declarative transaction attribute.</summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class UnitOfWorkAttribute : Attribute
    {
    }

    public interface IInputPort
    {
    }

    public interface IUseCase<in TInput, TResult> : IInputPort
    {
        Task<TResult> ExecuteAsync(TInput input, CancellationToken cancellationToken);
    }

    public interface INoResultUseCase<in TInput> : IInputPort
    {
        Task ExecuteAsync(TInput input, CancellationToken cancellationToken);
    }

    public interface IOutputPort
    {
    }

    /// <summary>The project's own supertype of every port that never leaves the process.</summary>
    public interface ILocalPort
    {
    }

    public interface IIntegrationEvent
    {
    }

    public interface IValue
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Retrofit.Application
{
    /// <summary>The host: assembles the application, named like a layer.</summary>
    public static class Host
    {
        public static void Run()
        {
        }
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Retrofit.Application.Config
{
    using Billing.Application.PayInvoice;

    public sealed class Wiring
    {
        public PayInvoiceApplicationService PayInvoice() => new(null!);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Retrofit.Common
{
    using Vocabulary;

    [SharedKernel]
    public static class CommonContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Retrofit.Common.Domain.Model
{
    using Vocabulary;

    public sealed record Money(long Cents) : IValue;
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Retrofit.Billing
{
    using Vocabulary;

    [BoundedContext("Billing", Description = "Invoices and their payment")]
    public static class BillingContext
    {
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Retrofit.Billing.Domain.Model
{
    using Common.Domain.Model;

    public sealed class Invoice
    {
        private readonly Money _total;

        public Invoice(Money total) => _total = total;

        public PaymentResult Pay(Money amount) =>
            amount.Cents >= _total.Cents
                ? new PaymentResult.Paid()
                : new PaymentResult.Short(_total.Cents - amount.Cents);
    }

    /// <summary>A closed outcome the domain returns: only its nested cases can derive from it.</summary>
    public abstract record PaymentResult
    {
        private PaymentResult()
        {
        }

        public sealed record Paid : PaymentResult;

        public sealed record Short(long MissingCents) : PaymentResult;
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Retrofit.Billing.Events
{
    using Vocabulary;

    [IntegrationEventType("billing.invoice-paid", Version = 2)]
    public sealed record InvoicePaid(string InvoiceId) : IIntegrationEvent;
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Retrofit.Billing.Application.Shared
{
    using Vocabulary;

    /// <summary>Never leaves the process.</summary>
    public interface ISecretHasher : IOutputPort, ILocalPort
    {
        string Hash(string secret);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Retrofit.Billing.Application.PayInvoice
{
    using Billing.Application.Shared;
    using Vocabulary;

    public sealed record PayInvoiceCommand(string InvoiceId, string Secret);

    public enum PayStatusResult
    {
        Paid,
        Short,
    }

    public interface IPayInvoiceUseCase : IUseCase<PayInvoiceCommand, PayStatusResult>
    {
    }

    [UnitOfWork]
    public sealed class PayInvoiceApplicationService : IPayInvoiceUseCase
    {
        private readonly ISecretHasher _hasher;

        public PayInvoiceApplicationService(ISecretHasher hasher) => _hasher = hasher;

        public Task<PayStatusResult> ExecuteAsync(PayInvoiceCommand input, CancellationToken cancellationToken) =>
            Task.FromResult(_hasher.Hash(input.Secret).Length == 0 ? PayStatusResult.Short : PayStatusResult.Paid);
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Retrofit.Billing.Adapter.Incoming.Web
{
    using System.Threading;
    using Billing.Application.PayInvoice;

    public sealed class InvoiceController
    {
        private readonly IPayInvoiceUseCase _payInvoice;

        public InvoiceController(IPayInvoiceUseCase payInvoice) => _payInvoice = payInvoice;

        public async Task<string> PayAsync(string id, string secret) =>
            (await _payInvoice.ExecuteAsync(new PayInvoiceCommand(id, secret), CancellationToken.None)).ToString();
    }
}

namespace DomainCentric.ArchRules.Tests.Fixtures.Retrofit.Billing.Adapter.Outgoing.Persistence
{
    /// <summary>The adapter's own abstraction over its tables — not a port.</summary>
    public interface IInvoiceRowStore
    {
        void Write(string row);
    }
}
