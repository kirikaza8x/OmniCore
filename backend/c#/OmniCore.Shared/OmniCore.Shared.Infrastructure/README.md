# OmniCore.Shared.Infrastructure

## Purpose
The technical plumbing every service reuses: strongly validated configuration classes, EF Core base classes (repository, unit of work, interceptors, specification evaluator, dynamic filter/sort), the message bus (MassTransit with RabbitMQ or Kafka, plus the transactional outbox), Redis/memory cache, MinIO/S3 storage, current-user and device detection, SignalR log streaming and small middleware. It implements the interfaces declared in `Shared.Application` and `Shared.Domain`.

## What is inside

### `DependencyInjection.AddSharedInfrastructure(configuration, moduleAssemblies?)`
Registration order (a service calls this first, then adds its own DbContext, see `Auth.Infrastructure/DependencyInjection.cs`):
1. `RegisterAllConfigurations`: every non-abstract `ConfigBase` subclass found in the Shared assembly **and the service's assemblies** is bound, DataAnnotations-validated and checked at startup (`AddConfig<T>`). So every service must supply the required values of *all* config classes it can see (for example `Storage__*` even in a service that never uploads).
2. Scrutor scan: any `IDomainEventDispatcher` implementation (scoped).
3. Scrutor scan: any `ISaveChangesInterceptor` (scoped), which is how the two interceptors below are picked up. The service's DbContext adds them with `options.AddInterceptors(...)`.
4. Global JSON enum-as-string converter; `AddControllers()` (see Known gaps).
5. `IDateTimeProvider` (singleton).
6. `AddCurrentUserService()` (HttpContextAccessor, `IDeviceDetectionService` singleton, `ICurrentUserService` scoped) and `AddStorageService()` (`IStorageService` singleton).
7. Caching (`ConfigureCaching`): Redis when `ConnectionStrings:Redis`, `Redis:ConnectionString` or `Redis:Host` is set (connect timeout and sync timeout 1 s, `AbortOnConnectFail=false`), else a distributed memory cache; `ICacheService` singleton.
8. `AddSignalR()`.
9. Quartz (`AddQuartz` + hosted service, waits for running jobs on shutdown). A service adds its own jobs with another `AddQuartz(...)` call (Auth does, for the refresh-token purge).

### `Configs/` (settings; environment variables use `__` for `:`)
| Class | Section | Required | Defaults and notes |
|---|---|---|---|
| `DatabaseConfig` | `Database` | `ConnectionString` | `MaxRetryCount` 3 (1-10), `CommandTimeout` 30 s (5-300), `EnableDetailedErrors` false, `EnableSensitiveDataLogging` false |
| `MessageBrokerConfig` | `MessageBroker` | none | `Provider` `RabbitMQ` or `Kafka` (anything else fails startup; `Both` was removed), `Host` localhost, `Username`/`Password` guest, `KafkaBootstrapServers` localhost:9092, `OutboxDbProvider` Postgres, `EnableOutbox` true (not allowed together with Kafka) |
| `RedisConfig` | `Redis` | `Host` | `Port` 6379, `Password`, `InstanceName` `OmniCore_` |
| `MemoryCacheConfig` | `MemoryCache` | none | `DefaultExpirationMinutes` 30 (1-1440), `SizeLimit`, `CompactionPercentage` 0.05, `ExpirationScanFrequencySeconds` 60 |
| `StorageConfig` | `Storage` | `Endpoint`, `AccessKey`, `SecretKey`, `BucketName` | `Region` us-east-1, `UseSSL` true, `PublicUrl` |
| `JwtConfig` | `Jwt` | `Issuer`, `Audience`, and one of `Secret` / `PrivateKeyPem` / `JwksUrl` | `ExpiryMinutes` 15, `RefreshTokenExpiryDays` 7, `MaxActiveSessions` 10. Full explanation: [docs/auth-tokens.md](../../../../docs/auth-tokens.md) |
| `CorsConfig` | `Cors` | none | `AllowedOrigins` (array), `AllowAnyOrigin` false |
| `RateLimitingConfig` | `RateLimiting` | none | `GlobalPolicy`, `Policies:<name>:PermitLimit/WindowSeconds/QueueLimit` (100 / 60 / 10) |
| `QdrantConfig` | `Qdrant` | none (Host defaults to localhost) | `Port` 6334, `Collections`, `Retry`. No code uses it yet |

### `Data/`
| Type | What it does |
|---|---|
| `Repositories/RepositoryBase<TEntity,TId>` (partial: `.Read`, `.Write`, `.Bulk`) | EF implementation of `IRepository`. `ApplySpecification(spec)` is the single place queries are built. Bulk methods use EFCore.BulkExtensions. `Upsert` detaches a tracked copy then `Update`s; `UpsertAsync(entity, spec)` copies values onto the existing row. |
| `Specifications/SpecificationEvaluator<T>` | Turns an `ISpecification` into IQueryable: no-tracking, criteria, includes, one order-by, skip/take. |
| `UnitOfWork/UnitOfWorkBase<TDbContext>` | `IUnitOfWork` over a DbContext: save, manual transaction, `ExecuteInTransactionAsync` (retry-safe through the EF execution strategy). Also a `Repository<TEntity,TId>()` factory. |
| `Interceptors/AuditableEntityInterceptor` | On save: sets `CreatedAt/By` and `ModifiedAt/By` for `IAuditableEntity` (user id from `ICurrentUserService`, `"System"` when none); for `ISoftDeletable` sets `IsActive = true` on add and turns a delete into `IsActive = false`. |
| `Interceptors/DispatchDomainEventsInterceptor` | Before the save, collects events from every tracked aggregate root, clears them and dispatches through `IDomainEventDispatcher`, repeating while handlers raise new events (max 10 rounds, then throws). Handlers therefore run **before** the commit, in the same save. |
| `Seeds/IDataSeeder` | `Order` and `SeedAllAsync()`; found by `AddDataSeeders` and run by `ApplyMigrationsAsync(seedData: true)`. |

### `EventBus/` and the broker extensions
| Type | What it does |
|---|---|
| `DomainEventDispatcher` | Publishes each `IDomainEvent` with MediatR `IPublisher` (in-process). |
| `PublishEndpointEventBus` / `EventBus` | `IEventBus` over MassTransit `IPublishEndpoint` (RabbitMQ). The two classes are identical; only `PublishEndpointEventBus` is registered. |
| `KafkaEventBus` | `IEventBus` over Kafka Rider producers: topic = event type name in kebab-case, key = `IPartitionedIntegrationEvent.PartitionKey` or the event id. |
| `IntegrationEventConsumer<T>` | The one MassTransit consumer: resolves all `IIntegrationEventHandler<T>` and runs them in parallel with `Task.WhenAll`. |
| `Extensions/MassTransitExtensions.AddMassTransitWithBroker<TDbContext>` | Validates `MessageBrokerConfig`, scans handlers (`IIntegrationEventHandler<>`, scoped), registers the `IEventBus`, kebab-case endpoint names, the EF **outbox** (RabbitMQ only; provider Postgres, SQL Server, MySQL or SQLite) and the transport. |
| `Extensions/RabbitMqExtensions` | Host (URI or host name), retry 3 times every 2 s, prefetch 16, endpoints configured per consumer. |
| `Extensions/KafkaExtensions` | Producer per event type, topic endpoint per handled event type, group id `<topic>-consumer`, plus an in-memory bus. Uses reflection. |
| `Extensions/EventBusReflectionExtensions` | `GetIntegrationEventTypes`, `GetHandledEventTypes`, `ToKebabCase`. |

### `Extensions/` (the rest)
| File | What it does |
|---|---|
| `OmniCoreConfigurationExtensions.AddConfig<T>` | Bind + validate on start. |
| `MigrationExtensions` | `ApplyMigrationsAsync<TContext>(seedData)` runs `Database.MigrateAsync()` and the seeders at startup. |
| `QueryableExtension` | `ToPagedResultAsync`, `ApplyPagination`, `ApplyDynamicFilters(Filter)` and `ApplyDynamicSorting(Sort[])` built on **System.Linq.Dynamic.Core** (string expressions). |
| `AuditConfigurationExtensions.ConfigureAuditProperties` | EF mapping helper for audit columns + `IsActive` (default true) on entities that are `IAuditableEntity` and `ISoftDeletable`. |
| `AuthenticationServiceExtensions`, `StorageServiceExtensions`, `CachingServiceExtensions.AddCacheServices`, `SignalRLoggingExtensions.AddSignalRLogger`, `DeviceIdMiddlewareExtensions.UseDeviceId` | Small registration helpers. |
| `HealthCheckExtensions`, `ResilienceExtensions` | Entirely commented out. |

### `Service/`, `Middleware/`, `Tracing/`, `Factories/`, `Security/`
| Type | What it does |
|---|---|
| `CurrentUserService` | Reads the caller from `HttpContext`: `UserId` (claim `NameIdentifier` or `sub`), e-mail, name, roles (`role` claims), `Jti`, IP (`RemoteIpAddress`), user agent, device id; `GetCurrentUser()` builds a `CurrentUserDto`. |
| `DeviceDetectionService` | UAParser: browser, OS, device name; generates device ids. |
| `CacheService` | Redis (or distributed memory) first, JSON-serialised (camelCase, ignores cycles); on any failure falls back to an in-process `IMemoryCache`. `GetOrCreateAsync` takes a per-key lock. `RemoveByPrefixAsync` scans Redis keys. |
| `MinioStorageService` | S3/MinIO client (`AWSSDK.S3`), creates the bucket on first use, upload (stream, bytes, `IFileUpload`), delete, download, public URL. |
| `LogHub`, `SignalRLogNotifier`, `SignalRLoggerProvider`/`SignalRLogger` | Streams Information+ log lines to all hub clients through a bounded channel (2000, drops oldest). The hub is mapped with `RequireAuthorization()` in `Shared.Api`. |
| `DateTimeProvider` | `DateTime.UtcNow`. |
| `Middleware/CorrelationIdMiddleware`, `Tracing/CorrelationIdAccessor` | Reads or creates `X-Correlation-ID` and echoes it. |
| `Middleware/DeviceIdMiddleware` | Reads or creates `X-Device-ID`, stores it in `HttpContext.Items`, echoes it. |
| `Factories/DesignTimeDbContextFactoryBase<T>` | Base for `dotnet ef`: builds the DbContext from `Database__*` environment variables (Npgsql, snake_case, history table in the service schema). |
| `Security/JwtKeySet` | RS256/HS256 key handling and JWKS fetch/cache: [docs/auth-tokens.md](../../../../docs/auth-tokens.md). |

## Depends on / used by
- Projects: `Shared.Application`, `Shared.Domain`. Frameworks/packages: ASP.NET Core shared framework, EF Core 9 + Npgsql, EFCore.BulkExtensions, EFCore.NamingConventions, MassTransit 8.3.6 (EF outbox, RabbitMQ, Kafka), Quartz 3.15, StackExchange Redis cache, AWSSDK.S3, UAParser, System.Linq.Dynamic.Core, Scrutor, Microsoft.AspNetCore.Authentication.JwtBearer (for `JwtKeySet`).
- Used by: `Shared.Api` and the Infrastructure layer of each service.

## Rules for this layer
- Implements interfaces from Application/Domain; no business rules here. A service's own repositories, DbContext, configurations and migrations stay in the service's Infrastructure project.
- Everything configurable comes from `ConfigBase` classes (environment variables), validated at startup; no `appsettings*.json` in the repo.
- Registration goes through the `Add...` extension methods; keep `AddSharedInfrastructure` free of service-specific code.

## How to ...
**Add a config class:** `public class FooConfig : ConfigBase { public override string SectionName => "Foo"; [Required] public string Url { get; set; } = ""; }` in this project or in the service's Infrastructure project. It is discovered and validated at startup; set `Foo__Url` in compose and `.env.example`.

**Add a repository (in a service):** `public sealed class OrderRepository(OrdersDbContext db) : RepositoryBase<Order, OrderId>(db), IOrderRepository { ... }`. Names ending in `Repository` are registered by the service's Scrutor scan (`Auth.Infrastructure/DependencyInjection.cs`).

**Add a Quartz job (in a service):** write an `IJob` (see `Auth.Infrastructure/Jobs/PurgeRefreshTokensJob.cs`) and in the service's DI call `services.AddQuartz(q => { q.AddJob<MyJob>(key); q.AddTrigger(t => t.ForJob(key).WithSimpleSchedule(...)); });`.

**Publish an integration event reliably:** with `EnableOutbox=true` (RabbitMQ), call `IEventBus.PublishAsync(event)` from a handler that runs inside the save (for example a domain-event handler such as `PublishAccountRegisteredIntegrationEventHandler`); the bus outbox writes it in the same transaction and a background delivery sends it. The event class goes in the service's `...Contracts` project.

**Add a data seeder:** implement `IDataSeeder` (see `Auth.Infrastructure/Persistence/seeds/RoleSeeder.cs`); `AddDataSeeders(assembly)` registers it and `UseAuthInfrastructureAsync(seedData: true)` runs it after the migrations.

## Known gaps
Ids are from `docs/repo-check.md`; new ones (`I1`..`I9`) are added there in the same commit.
- **I1 (new, latent, medium)** `ApplyDynamicFilters` and `ApplyDynamicSorting` paste the client-supplied `Field` (and the filter `Logic`) straight into a Dynamic LINQ string. A caller could filter or sort on any property of the entity graph (for example `PasswordHash.Value`, one prefix at a time) or inject `or` clauses. A parse error is swallowed and the **unfiltered** query is returned. Nothing exposes `AdvancedPagedQuery` yet, but fix it (allow-list of fields per query) before the first endpoint does. `PagedQuery.SortColumn` has the same property.
- **I2 (new)** `CacheService.RemoveByPrefixAsync` searches Redis for `<prefix>*`, but the Redis cache stores keys as `<InstanceName><key>` (`OmniCore_` or `OmniCore_Auth_`), so it matches nothing. Nobody calls it yet.
- **I3 (new)** Queue and consumer-group names are not service-scoped: RabbitMQ uses `SetKebabCaseEndpointNameFormatter()` with no prefix, Kafka uses `<topic>-consumer`. Two services that handle the same event would share one queue/group and split the messages instead of each receiving a copy. Add a per-service prefix / group id before the User service consumes `AccountRegisteredIntegrationEvent`.
- **I4 (new)** Dead or unwired code: `CorrelationIdMiddleware` + `CorrelationIdAccessor` (the accessor is not registered and nothing calls `UseMiddleware`), `UseDeviceId`, `AddSignalRLogger`, `AddCacheServices`, `EventBus` (duplicate of `PublishEndpointEventBus`), `HealthCheckExtensions` and `ResilienceExtensions` (fully commented out), `QdrantConfig`. The correlation id header is also used unvalidated if wired (length, characters).
- **I5 (new)** `AddSharedInfrastructure` calls `AddControllers()`, which no service uses (all endpoints are Carter modules).
- **I6 (new)** `IntegrationEventConsumer` runs all handlers of an event in parallel; if one throws, the retry (3 x 2 s) re-runs all of them, so handlers must be idempotent. The Kafka path has no retry or outbox at all.
- **I7 (new)** Four `Microsoft.CodeAnalysis.*` packages (Roslyn workspaces, MSBuild) are referenced and no code uses them. They may be pinned for EF tooling; check before removing, because they add size to every service image.
- **I8 (new)** `CurrentUserService.DeviceId` trusts the client's `X-Device-ID` header, so a device id is client-controlled.
- **S2** In-service rate limiting and `CurrentUserService.IpAddress` see only the nginx address (`RemoteIpAddress`) until forwarded headers are configured.
- **S10** The synchronous `SavingChanges` path of `DispatchDomainEventsInterceptor` blocks on async dispatch; the async path (used by the unit of work) is fine.
- **S13** `MinioStorageService` trusts the client's content type and file name; `FileSize` is 0 for non-seekable streams.
- **D1** `AuditableEntityInterceptor` conflates inactive and deleted (see `Shared.Domain/README.md`).
