# OmniCore.Shared.Domain

## Purpose
The vocabulary every service's Domain layer is written in: DDD base types (entity, aggregate root, domain event,
tenant), the `Result` / `Error` pattern used instead of exceptions for expected failures, small reusable value objects,
repository / unit-of-work / specification **interfaces**, and paging query types. It has no behaviour that touches a
database or the network: implementations live in `OmniCore.Shared.Infrastructure`.

## What is inside

### `DDD/` (base types)
| Type | What it does / when to use |
|---|---|
| `IEntity`, `IEntity<TId>`, `Entity<TId>` | Base for anything with an identity. Equality is by type + `Id`; an entity whose `Id` is still `default` is never equal to another. `Id` is `init`-only. |
| `EntityId<TValue>` | `abstract record` base for strongly typed ids (`AccountId`, `RoleId`, ...). |
| `IAggregateRoot`, `AggregateRoot<TId>` | An entity that is a consistency boundary and collects domain events: `RaiseDomainEvent(e)` (protected), `DomainEvents`, `ClearDomainEvents()`. Only aggregate roots can be used with `IRepository`. |
| `IDomainEvent`, `DomainEvent` | In-process event. `DomainEvent` is an abstract `record` with `EventId`, `OccurredOn` (UTC) and `EventType` (the type name). `IDomainEvent` extends MediatR `INotification`. |
| `IAuditableEntity` | `CreatedAt/By`, `ModifiedAt/By`; filled automatically by `AuditableEntityInterceptor` (Infrastructure). |
| `ISoftDeletable` | Only member is `bool IsActive`. See Known gaps. |
| `ITenantScoped`, `TenantId`, `TenantEntity<TId>`, `TenantAggregateRoot<TId>` | Multi-tenancy building blocks. `TenantId` is a `readonly record struct` that trims and lower-cases its value and throws on empty. Only the User service's `UserTenantProfile` uses them today (ADR 0001 is still *proposed*). |

### `Abstractions/` (results, errors, guards)
| Type | What it does / when to use |
|---|---|
| `Result`, `Result<T>` | Success/failure return type. `Result.Success()`, `Result.Failure(error)`, `Result.Success(value)`, `Result.Failure<T>(error)`. Implicit conversions from `Error` and from a value (null value becomes `Error.NullValue`). `Value` throws `InvalidOperationException` on a failure, so check `IsFailure` first. `Match(...)` helper. A success with an error (or failure without one) throws in the constructor. |
| `Error`, `ErrorType` | Record `(Code, Description, Type)` with factories `Failure`, `Validation`, `NotFound`, `Conflict`, `Unauthorized`, `Forbidden`, plus `Error.None`. The Api layer maps `ErrorType` to an HTTP status. Codes follow `Entity.Reason` (e.g. `Account.NoEmail`). |
| `IValidationResult`, `ValidationResult`, `ValidationResult<T>` | A failed `Result` carrying a list of `Error`s; produced by the validation pipeline behaviour. |
| `Guard` | Throwing argument checks: `AgainstNull`, `AgainstNullOrEmpty`, `AgainstNegative`. For programmer errors, not for user input (use `Result` for that). **Not used anywhere yet.** |
| `DomainException` | Abstract exception carrying an `Error`. **Not used anywhere yet.** |
| `OmniCoreApplicationException` | Exception with `RequestName` and optional `Error`. Lives in this folder but its namespace is `OmniCore.Shared.Application.Exceptions`; not used anywhere yet. |

### `ValueObject/` (value objects)
| Type | Notes |
|---|---|
| `ValueObject` (abstract class, namespace `...Domain.ValueObject`) | Equality by `GetAtomicValues()`. The Auth and User services' ids and `Username` / `Email` derive from it. |
| `EmailAddress` | `Create(string?)` trims, lower-cases, checks a simple regex (`x@y.z`). Used by `Account`. |
| `PhoneNumber` | `Create(string?)` strips spaces, dashes and brackets, then requires E.164 (`+?[1-9]` followed by 1 to 14 digits). Used by `UserProfile`. |
| `Address`, `DateRange`, `Money` (+ `Currency` enum: USD, EUR, GBP, JPY, AUD, CAD) | Written as sealed records with a private constructor and `Create(...)` returning `Result`. `Money` supports `Add`/`Subtract` with a currency check and forbids negative amounts. **Not used by any service yet.** |

### `Data/` (persistence contracts, implemented in Infrastructure)
| Type | What it does |
|---|---|
| `IReadRepository<TEntity,TId>` | `GetByIdAsync`, `FirstOrDefaultAsync(spec)`, `ListAsync(spec)`, `GetPagedAsync`, `GetAdvancedPagedAsync`, `CountAsync`, `AnyAsync`, `ExistsAsync`. |
| `IBulkRepository<TEntity,TId>` | `BulkInsertAsync/UpdateAsync/DeleteAsync/MergeAsync`. |
| `IRepository<TEntity,TId>` | Both of the above plus `Add`, `AddRange`, `Update`, `Remove`, `RemoveRange`, `Upsert`, `UpsertAsync(entity, spec)`. `TEntity` must be an `AggregateRoot<TId>`. |
| `IUnitOfWork` | `SaveChangesAsync`, `BeginTransactionAsync`, `CommitTransactionAsync`, `RollbackTransactionAsync`, `ExecuteInTransactionAsync`. |
| `ISpecification<T>` / `BaseSpecification<T>` | Query description: `Criteria`, `Includes` (expressions and strings), one `OrderBy` or `OrderByDescending`, `Skip`/`Take`/`IsPagingEnabled`, `IsNoTracking`, `IsSatisfiedBy(entity)`. Subclasses call the protected `AddCriteria` (several calls are AND-ed), `AddInclude`, `AddOrderBy`, `ApplyPaging`, `ApplyNoTracking`. |

Note the namespaces: the files are in `Data/...` folders but declare `OmniCore.Shared.Domain.Repositories` and `...Domain.Specifications`.

### `Queries/` and `Pagination/`
| Type | What it does |
|---|---|
| `PagedQuery` | Page number (min 1), page size (1 to 100, default 10), `SortColumn` (default `"CreatedAt"`), `SortOrder` (default descending), `Skip`. |
| `AdvancedPagedQuery` | Same paging plus `SearchTerm`, `Sorts` (list of `Sort(Field, Order)`) and a nested `Filter` (`Field`, `Operator`, `Value`, `Logic` "and"/"or", child `Filters`). |
| `IPageable`, `ISortable`, `Sort`, `SortOrder`, `Filter` | The pieces above. |
| `DomainPagedResult<T>` | `Items`, `PageNumber`, `PageSize`, `TotalCount`, computed `TotalPages`, `HasNext`, `HasPrevious`, start/end index; `Map(...)` converts the items. |

## Depends on / used by
- Depends on: only the `MediatR` 12.2.0 package (for `INotification`). No project references.
- Used by: every other project in the solution (Shared.Application, Shared.Infrastructure, Shared.Api and the Domain layers of the Auth and User services).

## Rules for this layer
- Pure and persistence-ignorant: no EF Core, HTTP, logging or configuration types. (`MediatR` is the one exception, see Known gaps.)
- Expected failures return `Result` / `Error`; exceptions are for bugs. Factories on entities and value objects are `static Result<T> Create(...)` with a private constructor.
- Only aggregate roots get repositories. Child entities (e.g. `RefreshToken`) are reached through their root.
- Domain events are raised inside aggregates (`RaiseDomainEvent`) and dispatched after save by `DispatchDomainEventsInterceptor` (Infrastructure).
- This project holds generic types only; anything specific to one service belongs in that service's Domain project (`CLAUDE.md`, Conventions).

## How to ...
**Add a strongly typed id and an aggregate**
1. `public sealed record OrderId(Guid Value) : EntityId<Guid>(Value) { public static OrderId New() => new(Guid.NewGuid()); }`
2. `public class Order : AggregateRoot<OrderId> { private Order() {} ... }` with a private constructor for EF and a `public static Result<Order> Create(...)`.
3. In a method that changes state, call `RaiseDomainEvent(new OrderPlacedDomainEvent(...))` (a `record` deriving from `DomainEvent`).

**Add a query specification**
```csharp
public sealed class ActiveOrdersSpec : BaseSpecification<Order>
{
    public ActiveOrdersSpec() { AddCriteria(o => o.IsActive); AddInclude(o => o.Lines); ApplyNoTracking(); }
}
```
Pass it to `repository.ListAsync(new ActiveOrdersSpec())`.

**Return an error from a domain method**
`return Error.Conflict("Order.AlreadyPaid", "The order is already paid.");` (implicit conversion to `Result`), and the Api layer turns it into a 409.

## Known gaps
Verified in the code. Ids are the ones in `docs/repo-check.md`; "new" ones are added there as `D1`..`D6` in the same commit.
- **D1 (new)** `ISoftDeletable` is only `IsActive`. `AuditableEntityInterceptor` turns a hard delete into `IsActive = false` and sets `IsActive = true` on every added entity, but `Account` also has its own `IsDeleted` / `DeletedAtUtc` that the interceptor never touches. "Deactivated" and "deleted" are one flag in the interface and two in the entity. Entities and interface should agree (see `docs/entities.md`).
- **D2 (new)** `PagedQuery.SortColumn` defaults to `"CreatedAt"`, but entities that are not `IAuditableEntity` (for example `Role`) have no such column, so a default-sorted paged query on them would fail or be ignored depending on how Infrastructure resolves the column.
- **D3 (new)** `IDomainEvent : INotification` ties the Domain project to MediatR. Harmless today (the dispatcher also uses MediatR) but it is the one framework dependency in an otherwise pure layer.
- **D4 (new)** Namespaces do not match folders: `Data/Repositories` and `Data/UnitOfWork` declare `...Domain.Repositories`, `Data/Specifications` declares `...Domain.Specifications`, `ValueObject/ValueObject.cs` declares `...Domain.ValueObject` while the other value objects declare `...Domain.ValueObjects`, and `OmniCoreApplicationException` (in `Abstractions/`) declares `OmniCore.Shared.Application.Exceptions`. This confuses `using` lines and IDE navigation.
- **D5 (new)** Two value-object styles coexist: an abstract `ValueObject` class (Auth/User) and sealed `record`s (`EmailAddress`, `PhoneNumber`, `Address`, `Money`, `DateRange`). Pick one for new code (records are shorter and get equality for free).
- **D6 (new)** Dead code: `Guard`, `DomainException`, `OmniCoreApplicationException`, `Address`, `Money`, `DateRange`, `ITenantScoped`/`TenantEntity` are not referenced by any service. Keep only what ADR 0001 (tenancy) or a real feature needs.
- `TenantId` is a struct, so `default(TenantId)` has a `null` `Value` and bypasses the constructor check; `Specification` supports a single order-by only (no `ThenBy`); `DomainPagedResult.Empty` hard-codes page 1 / size 10.
- `Guard.AgainstNegative<T>` compares with `default!`, which throws a `NullReferenceException` for a reference-type `T` (works for numbers).
- S10 (see `docs/repo-check.md`) concerns the sync `DispatchDomainEventsInterceptor`, which depends on `IDomainEvent` defined here.
