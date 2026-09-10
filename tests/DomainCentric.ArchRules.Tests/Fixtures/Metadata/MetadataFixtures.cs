using System;
using DomainCentric.BuildingBlocks.Ddd.Tactical;

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Container { [AttributeUsage(AttributeTargets.All)] public class ClassifiedAttribute : Attribute {} }
namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Persistence { [AttributeUsage(AttributeTargets.All)] public class ClassifiedAttribute : Attribute {} }
namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Transaction { [AttributeUsage(AttributeTargets.All)] public class ClassifiedAttribute : Attribute {} }
namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection { [AttributeUsage(AttributeTargets.All)] public class ClassifiedAttribute : Attribute {} }
namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles { public class UnknownAttribute : Attribute {} public class ComposedAttribute : DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Container.ClassifiedAttribute {} }
namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Container.Classified]
public sealed class ModelTypeContainer {
public int Value;
public int Property {get;set;}
public ModelTypeContainer() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Persistence.Classified]
public sealed class ModelTypePersistence {
public int Value;
public int Property {get;set;}
public ModelTypePersistence() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Transaction.Classified]
public sealed class ModelTypeTransaction {
public int Value;
public int Property {get;set;}
public ModelTypeTransaction() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class ModelFieldInjection {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public int Value;
public int Property {get;set;}
public ModelFieldInjection() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class ModelFieldPersistence {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Persistence.Classified]
public int Value;
public int Property {get;set;}
public ModelFieldPersistence() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class ModelPropertyInjection {
public int Value;
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public int Property {get;set;}
public ModelPropertyInjection() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class ModelPropertyPersistence {
public int Value;
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Persistence.Classified]
public int Property {get;set;}
public ModelPropertyPersistence() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class ModelMethodTransaction {
public int Value;
public int Property {get;set;}
public ModelMethodTransaction() {}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Transaction.Classified]
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class ModelMethodInjection {
public int Value;
public int Property {get;set;}
public ModelMethodInjection() {}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class ModelConstructorInjection {
public int Value;
public int Property {get;set;}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public ModelConstructorInjection() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Composed]
public sealed class ModelTypeComposed {
public int Value;
public int Property {get;set;}
public ModelTypeComposed() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class ModelKeyProperty { [System.ComponentModel.DataAnnotations.Key] public int Id {get;set;} }}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Good.Module.Domain.Model {
public sealed class ModelRequiredProperty { [System.ComponentModel.DataAnnotations.Required] public string Name {get;set;} = ""; }
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public sealed class ModelAllUnknown {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public int Value;
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public int Property {get;set;}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public ModelAllUnknown() {}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Container.Classified]
public sealed class EventTypeContainer : IDomainEvent {
public int Value;
public int Property {get;set;}
public EventTypeContainer() {}
public void Operation() {}
public Guid EventId => Guid.Empty; public DateTimeOffset OccurredOn => DateTimeOffset.MinValue;
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Persistence.Classified]
public sealed class EventTypePersistence : IDomainEvent {
public int Value;
public int Property {get;set;}
public EventTypePersistence() {}
public void Operation() {}
public Guid EventId => Guid.Empty; public DateTimeOffset OccurredOn => DateTimeOffset.MinValue;
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Transaction.Classified]
public sealed class EventTypeTransaction : IDomainEvent {
public int Value;
public int Property {get;set;}
public EventTypeTransaction() {}
public void Operation() {}
public Guid EventId => Guid.Empty; public DateTimeOffset OccurredOn => DateTimeOffset.MinValue;
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class EventFieldInjection : IDomainEvent {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public int Value;
public int Property {get;set;}
public EventFieldInjection() {}
public void Operation() {}
public Guid EventId => Guid.Empty; public DateTimeOffset OccurredOn => DateTimeOffset.MinValue;
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class EventFieldPersistence : IDomainEvent {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Persistence.Classified]
public int Value;
public int Property {get;set;}
public EventFieldPersistence() {}
public void Operation() {}
public Guid EventId => Guid.Empty; public DateTimeOffset OccurredOn => DateTimeOffset.MinValue;
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class EventPropertyInjection : IDomainEvent {
public int Value;
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public int Property {get;set;}
public EventPropertyInjection() {}
public void Operation() {}
public Guid EventId => Guid.Empty; public DateTimeOffset OccurredOn => DateTimeOffset.MinValue;
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class EventPropertyPersistence : IDomainEvent {
public int Value;
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Persistence.Classified]
public int Property {get;set;}
public EventPropertyPersistence() {}
public void Operation() {}
public Guid EventId => Guid.Empty; public DateTimeOffset OccurredOn => DateTimeOffset.MinValue;
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class EventMethodTransaction : IDomainEvent {
public int Value;
public int Property {get;set;}
public EventMethodTransaction() {}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Transaction.Classified]
public void Operation() {}
public Guid EventId => Guid.Empty; public DateTimeOffset OccurredOn => DateTimeOffset.MinValue;
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class EventConstructorInjection : IDomainEvent {
public int Value;
public int Property {get;set;}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public EventConstructorInjection() {}
public void Operation() {}
public Guid EventId => Guid.Empty; public DateTimeOffset OccurredOn => DateTimeOffset.MinValue;
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Composed]
public sealed class EventTypeComposed : IDomainEvent {
public int Value;
public int Property {get;set;}
public EventTypeComposed() {}
public void Operation() {}
public Guid EventId => Guid.Empty; public DateTimeOffset OccurredOn => DateTimeOffset.MinValue;
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Good.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public sealed class EventAllUnknown : IDomainEvent {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public int Value;
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public int Property {get;set;}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public EventAllUnknown() {}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public void Operation() {}
public Guid EventId => Guid.Empty; public DateTimeOffset OccurredOn => DateTimeOffset.MinValue;
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Container.Classified]
public sealed class ServiceTypeContainer : IDomainService {
public int Value;
public int Property {get;set;}
public ServiceTypeContainer() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Persistence.Classified]
public sealed class ServiceTypePersistence : IDomainService {
public int Value;
public int Property {get;set;}
public ServiceTypePersistence() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Transaction.Classified]
public sealed class ServiceTypeTransaction : IDomainService {
public int Value;
public int Property {get;set;}
public ServiceTypeTransaction() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class ServiceFieldInjection : IDomainService {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public int Value;
public int Property {get;set;}
public ServiceFieldInjection() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class ServiceFieldPersistence : IDomainService {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Persistence.Classified]
public int Value;
public int Property {get;set;}
public ServiceFieldPersistence() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class ServicePropertyInjection : IDomainService {
public int Value;
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public int Property {get;set;}
public ServicePropertyInjection() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class ServicePropertyPersistence : IDomainService {
public int Value;
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Persistence.Classified]
public int Property {get;set;}
public ServicePropertyPersistence() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class ServiceMethodTransaction : IDomainService {
public int Value;
public int Property {get;set;}
public ServiceMethodTransaction() {}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Transaction.Classified]
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class ServiceMethodInjection : IDomainService {
public int Value;
public int Property {get;set;}
public ServiceMethodInjection() {}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class ServiceConstructorInjection : IDomainService {
public int Value;
public int Property {get;set;}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public ServiceConstructorInjection() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Composed]
public sealed class ServiceTypeComposed : IDomainService {
public int Value;
public int Property {get;set;}
public ServiceTypeComposed() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Good.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public sealed class ServiceAllUnknown : IDomainService {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public int Value;
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public int Property {get;set;}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public ServiceAllUnknown() {}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Container.Classified]
public sealed class FactoryTypeContainer : IFactory {
public int Value;
public int Property {get;set;}
public FactoryTypeContainer() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Persistence.Classified]
public sealed class FactoryTypePersistence : IFactory {
public int Value;
public int Property {get;set;}
public FactoryTypePersistence() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Transaction.Classified]
public sealed class FactoryTypeTransaction : IFactory {
public int Value;
public int Property {get;set;}
public FactoryTypeTransaction() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class FactoryFieldInjection : IFactory {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public int Value;
public int Property {get;set;}
public FactoryFieldInjection() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class FactoryFieldPersistence : IFactory {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Persistence.Classified]
public int Value;
public int Property {get;set;}
public FactoryFieldPersistence() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class FactoryPropertyInjection : IFactory {
public int Value;
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public int Property {get;set;}
public FactoryPropertyInjection() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class FactoryPropertyPersistence : IFactory {
public int Value;
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Persistence.Classified]
public int Property {get;set;}
public FactoryPropertyPersistence() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class FactoryMethodTransaction : IFactory {
public int Value;
public int Property {get;set;}
public FactoryMethodTransaction() {}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Transaction.Classified]
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class FactoryMethodInjection : IFactory {
public int Value;
public int Property {get;set;}
public FactoryMethodInjection() {}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class FactoryConstructorInjection : IFactory {
public int Value;
public int Property {get;set;}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public FactoryConstructorInjection() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Composed]
public sealed class FactoryTypeComposed : IFactory {
public int Value;
public int Property {get;set;}
public FactoryTypeComposed() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Good.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public sealed class FactoryAllUnknown : IFactory {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public int Value;
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public int Property {get;set;}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public FactoryAllUnknown() {}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Container.Classified]
public sealed class SpecTypeContainerSpecification {
public int Value;
public int Property {get;set;}
public SpecTypeContainerSpecification() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Persistence.Classified]
public sealed class SpecTypePersistenceSpecification {
public int Value;
public int Property {get;set;}
public SpecTypePersistenceSpecification() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Transaction.Classified]
public sealed class SpecTypeTransactionSpecification {
public int Value;
public int Property {get;set;}
public SpecTypeTransactionSpecification() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class SpecFieldInjectionSpecification {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public int Value;
public int Property {get;set;}
public SpecFieldInjectionSpecification() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class SpecFieldPersistenceSpecification {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Persistence.Classified]
public int Value;
public int Property {get;set;}
public SpecFieldPersistenceSpecification() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class SpecPropertyInjectionSpecification {
public int Value;
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public int Property {get;set;}
public SpecPropertyInjectionSpecification() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class SpecPropertyPersistenceSpecification {
public int Value;
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Persistence.Classified]
public int Property {get;set;}
public SpecPropertyPersistenceSpecification() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class SpecMethodTransactionSpecification {
public int Value;
public int Property {get;set;}
public SpecMethodTransactionSpecification() {}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Transaction.Classified]
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class SpecMethodInjectionSpecification {
public int Value;
public int Property {get;set;}
public SpecMethodInjectionSpecification() {}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
public sealed class SpecConstructorInjectionSpecification {
public int Value;
public int Property {get;set;}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Injection.Classified]
public SpecConstructorInjectionSpecification() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Bad.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Composed]
public sealed class SpecTypeComposedSpecification {
public int Value;
public int Property {get;set;}
public SpecTypeComposedSpecification() {}
public void Operation() {}
}}

namespace DomainCentric.ArchRules.Tests.Fixtures.Metadata.Good.Module.Domain.Model {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public sealed class SpecAllUnknownSpecification {
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public int Value;
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public int Property {get;set;}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public SpecAllUnknownSpecification() {}
[DomainCentric.ArchRules.Tests.Fixtures.Metadata.Roles.Unknown]
public void Operation() {}
}}
