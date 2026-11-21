# OmniCore.Shared.Application

## Purpose
The application-layer toolkit shared by all services: the CQRS marker interfaces (`ICommand`, `IQuery`, handlers), the MediatR pipeline behaviours that wrap every request (logging, validation, save), the **interfaces** for cross-cutting services (current user, cache, event bus, storage, e-mail, time, tracing) and a few DTOs and helpers. It defines *what* a service needs; the implementations are in `OmniCore.Shared.Infrastructure`.

## What is inside

### `Abstractions/Messaging/` (CQRS)
| Type | Use |
|---|---|
| `ICommand` / `ICommand<T>` | A write request. Returns `Result` / `Result<T>` (both are `IRequest<...>` and `IBaseCommand`). |
| `ICommandHandler<TCommand>` / `<TCommand,TResponse>` | Handler for the above. |
| `IQuery<T>` / `IQueryHandler<TQuery,T>` | A read request returning `Result<T>`. |
| `IBaseCommand` | Marker. The save-on-success behaviour applies only to requests carrying it. |
| `ITransactionalCommand`, `IStreamCommand`, `IStreamCommandHandler` | Declared but not used by any service or behaviour today. |
| `IDomainEventHandler<TEvent>` | Handler for an in-process domain event. You implement `HandleAsync`; a default interface method adapts it to MediatR's `Handle`. |

### `Behaviors/` (MediatR pipeline)
Registered in `AddSharedApplication`, outermost first:

| Order | Behaviour | What it does |
|---|---|---|
| 1 | `LoggingBehavior` | Logs start and end with elapsed ms, tags the OpenTelemetry activity (`request.module`, `request.name`), logs the `Error` of a failed result at warning level. |
| 2 | `ValidationBehavior` | Runs every FluentValidation `IValidator<TRequest>`; on failures returns a `ValidationResult` / `ValidationResult<T>` with one `Error.Validation(propertyName, message)` each, without calling the handler. |
| 3 | `UnitOfWorkBehavior` | **Commands only** (`IBaseCommand`). Runs the handler, and calls `IUnitOfWork.SaveChangesAsync` only when the result is a success. A handler that must persist something on a failure result has to save itself (refresh-token reuse fix, finding A1). |
| 4 | `PerformanceBehavior` | Warns when a request takes more than 500 ms. Sits innermost, so it times the handler only, not validation or the save. |
| not registered | `CachingBehavior` | `internal` and never added; see Known gaps. |

### `Abstractions/` (service contracts, implemented in Infrastructure)
| Folder / type | Use |
|---|---|
| `Authentication/ICurrentUserService` | `UserId`, `Email`, `Name`, `IsAuthenticated`, `Roles`, `Jti`, `IpAddress`, `UserAgent`, `DeviceId`, `IsInRole`. The caller's identity comes from here (the token), never from a request body. |
| `Authentication/IDeviceDetectionService` | Parses a user agent into `DeviceInfo`, generates a device id. |
| `Caching/ICacheService`, `ICacheableQuery<T>` | Get/Set/Remove/`RemoveByPrefixAsync`/`GetOrCreateAsync`/Exists. A query declares `CacheKey` and optional `Expiration`. |
| `EventBus/IEventBus`, `IDomainEventDispatcher` | Publish integration events to the broker; dispatch domain events after save. |
| `OutBox/IOutboxService` | `EnqueueAsync(integrationEvent)`; declared, nothing uses it directly. |
| `Storage/IStorageService`, `IFileUpload`, `UploadResult` | Upload (file, stream or bytes), delete, download, public URL. |
| `Notifications/IEmailSender`, `EmailMessage` | E-mail contract. **No implementation exists.** |
| `SignalR/ILogNotifier` | Pushes log lines to the SignalR log hub. |
| `Time/IDateTimeProvider` | `UtcNow`, so time can be faked in tests. |
| `Tracing/ICorrelationIdAccessor` | Current request's correlation id. |

### Other
| Type | Use |
|---|---|
| `DTOs/BaseDto<TId>`, `AuditableDto<TId>`, `CurrentUserDto`, `DeviceInfo`, `LogNotificationDto` | Shared response shapes. |
| `DTOs/PagedRequest` (+ `FilterRequest`, `SortRequest`) | HTTP-facing paging shape: page size clamped to 1-100 (default 10), `SortColumn` default `"CreatedAt"`, `SortDirection` default `"desc"`. Not used by any service yet. |
| `Helpers/CacheKeyGeneratorHelper` | `For<T>(ids...)` returns `TypeName:id1:id2`; `PrefixFor<T>()` returns `TypeName:`. |
| `Helpers/SlugHelper.Generate` | URL slug: strips accents, lower-cases, non-alphanumerics become `-`. Not used yet. |
| `DependencyInjection.AddSharedApplication(params Assembly[])` | Registers MediatR (scanning this assembly plus the given ones), the four behaviours, AutoMapper and all FluentValidation validators (internal ones included). |
| `AssemblyReference` | `Assembly` handle used for scanning. |

## Depends on / used by
- Packages: MediatR 12.2.0, FluentValidation 12.1.0 (+ DI extensions), AutoMapper 16.1.1, Microsoft.Extensions.Logging, Serilog 4.3.0.
- Projects: `Shared.Domain`, `Shared.Contracts`.
- Used by: `Shared.Infrastructure` and `Shared.Api`, and the Application layer of Auth and User.

## Rules for this layer
- No EF Core, ASP.NET or broker types. Only interfaces and plain DTOs; implementations go to Infrastructure.
- A handler returns `Result`; do not throw for expected failures. Validate input in a `...Validator` next to the command, not in the handler.
- Commands go through the unit-of-work behaviour, so a handler should not call `SaveChangesAsync` itself unless it must persist on a failure result.
- Take the caller's id from `ICurrentUserService`, never from the request (finding U3).

## How to ...
**Add a command**
1. `public record RenameRoleCommand(Guid RoleId, string Name) : ICommand;` (or `ICommand<RoleResponse>`).
2. `public sealed class RenameRoleCommandHandler(IRoleRepository roles) : ICommandHandler<RenameRoleCommand> { public async Task<Result> Handle(RenameRoleCommand c, CancellationToken ct) { ... return Result.Success(); } }`
3. `public sealed class RenameRoleCommandValidator : AbstractValidator<RenameRoleCommand> { ... }` (found automatically, internal or public).
4. The endpoint sends it with `ISender`; nothing to register.

**Add a query**: `public record GetXQuery(Guid Id) : IQuery<XResponse>;` plus an `IQueryHandler<GetXQuery, XResponse>`.

**React to a domain event**: `public sealed class Foo : IDomainEventHandler<AccountCreatedDomainEvent> { public Task HandleAsync(AccountCreatedDomainEvent e, CancellationToken ct) {...} }` (runs in the same save, dispatched by the EF interceptor).

**Expose a new cross-cutting service**: add the interface here, the implementation in `Shared.Infrastructure/Service/...`, register it in `AddSharedInfrastructure`.

## Known gaps
Ids from `docs/repo-check.md`; new ones (`AP1`..) are added there in the same commit.
- **S6** `CachingBehavior` is not registered. **AP1 (new)**: it could not work even if it were. Its constraint is `TRequest : ICacheableQuery<TResponse>` with `TResponse : Result`, but a query like `GetAccountByIdQuery : ICacheableQuery<AccountResponse>` is an `IRequest<Result<AccountResponse>>`, so for the pipeline `TResponse` is `Result<AccountResponse>` and the constraint can never be satisfied. It would also try to cache a `Result<T>` object, which has no public constructor to deserialize from.
- **AP2 (new)** Cache invalidation keys do not match query keys: `GetAccountByIdQuery` uses `CacheKeyGeneratorHelper.For<...>` (`GetAccountByIdQuery:<id>`), while `AccountCacheInvalidationEventHandler` removes `auth:account:id:<id>`. Fix both together when caching is turned on.
- **S12** `ITransactionalCommand` is unused; every command saves once at the end with no wrapping transaction.
- **AP3 (new)** `PerformanceBehavior` keeps a `Stopwatch` as an instance field and never resets it. That is correct only while the behaviour is created per request (transient); a change of lifetime would add up times.
- **AP4 (new)** Unused code: `IEmailSender`/`EmailMessage` (no implementation), `IOutboxService`, `IStreamCommand*`, `PagedRequest`/`FilterRequest`/`SortRequest`, `BaseDto`/`AuditableDto`, `SlugHelper`. `PagedRequest.PageNumber` is not clamped (negative values pass) unlike `PagedQuery` in Shared.Domain, and its `"CreatedAt"` sort default has the same problem as D2.
- **AP5 (new)** The `Serilog` package reference is not used by any code in this project (it logs through `ILogger`), and `Shared.Application` keeps `AutoMapper` while the Auth/User code also has hand-written mapping extensions: two mapping styles.
- Error code for validation failures is the *property name* (`Error.Validation(f.PropertyName, ...)`), so clients cannot tell a property name from a business error code.
