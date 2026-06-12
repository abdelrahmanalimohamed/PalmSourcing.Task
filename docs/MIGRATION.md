## Objective

Migrate the legacy `OrderProcessor` from the existing .NET Framework 4.7 WebForms application to a modern .NET 8 implementation without downtime and without breaking existing consumers (Razor Pages, ASMX services, and other integrations).

## Approach

I would use a Strangler Fig migration pattern.

1. Introduce an `IOrderProcessor` abstraction in the current application.
2. Wrap the existing `OrderProcessor` behind that interface so all callers depend on the abstraction rather than the concrete implementation.
3. Build a new .NET 8 implementation with clear separation of concerns:

   * Repositories for data access
   * Pricing service for business rules
   * Payment client for external payment integration
   * Email service for notifications
4. Deploy the new implementation behind a feature flag.
5. Route a small percentage of traffic to the new implementation and compare outcomes against the legacy processor.
6. Gradually increase traffic as confidence grows.
7. Remove the legacy implementation after validation and monitoring confirm functional parity.

## Why This Fits The Context

The system cannot go offline and multiple parts of the application currently depend directly on `OrderProcessor`.

A gradual migration minimizes operational risk, enables rollback, and allows the team to validate business behavior in production without a large cutover event.

The new implementation also improves:

* Testability
* Maintainability
* Dependency management
* Separation of business rules from infrastructure concerns

## Key Risk

The largest risk is not the technical migration itself but undocumented business logic hidden within the remaining 1,760 lines of the legacy `OrderProcessor`.

There may be school-specific pricing rules, inventory exceptions, payment edge cases, or historical behaviors that are not visible in the provided code sample. Before full migration, I would allocate time for rule discovery, regression testing, and side-by-side result validation to ensure business behavior is preserved.
