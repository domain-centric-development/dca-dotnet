namespace DomainCentric.BuildingBlocks.Ddd.Tactical;

/// <summary>
/// Marker interface for Domain Services.
/// </summary>
/// <remarks>
/// <para>
/// Domain Services are stateless operations that don't naturally belong to an Entity or Value
/// Object. They encapsulate domain logic that involves multiple domain objects or doesn't fit within
/// a single Aggregate.
/// </para>
/// <para><b>Characteristics:</b></para>
/// <list type="bullet">
///   <item><description>Stateless (only read-only fields for dependencies)</description></item>
///   <item><description>Named after activities or actions (e.g., <c>PricingService</c>, <c>CartTotalCalculator</c>)</description></item>
///   <item><description>Express domain concepts in the Ubiquitous Language</description></item>
///   <item><description>Carry no framework attributes or dependencies</description></item>
///   <item><description>Instantiated by the application layer</description></item>
/// </list>
/// <para><b>Examples:</b></para>
/// <list type="bullet">
///   <item><description>Calculating cart totals with complex tax rules (involves multiple items)</description></item>
///   <item><description>Applying pricing rules and discounts (domain logic not belonging to a single entity)</description></item>
///   <item><description>Validating business constraints that span multiple aggregates</description></item>
/// </list>
/// <para>
/// <b>Reference:</b> Eric Evans' Domain-Driven Design (2003), Chapter 5: "A Model Expressed in Software".
/// </para>
/// </remarks>
public interface IDomainService
{
}
