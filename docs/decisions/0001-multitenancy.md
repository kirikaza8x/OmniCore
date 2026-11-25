# ADR 0001: Multi-tenancy for Auth and User

- Status: **accepted by delegation 2026-11-24** (owner: "finish all before a 4th service"). Step 1 (Shared) written, unbuilt; steps 2-4 not started. Plan: `docs/plans/0007-multitenancy.md`.
- Date: 2026-11-19
- Scope: `OmniCore.Shared`, `OmniCore.Services.Auth`, `OmniCore.Services.User`, the gateway
- Basis: a read of the code (see `docs/repo-check.md`). Nothing was built or run.

## Context: what exists today

- **Auth** has one global `Account` (username, email, password hash, `IsActive`) with roles through `AccountRole`
  (`User`, `Admin`, `SuperAdmin`, seeded). The access token carries `sub`, `jti`, `email`, `name` and role claims,
  HS256 with one shared secret. There is no tenant anywhere in Auth.
- **Shared.Domain** already has `TenantId` (a struct wrapping a string), `ITenantScoped` and
  `TenantAggregateRoot<TId>`. Nothing uses them except User. There is **no `ITenantContext`, no EF query filter,
  no tenant on integration events, no tenant claim**.
- **User** is design-only (U1 in repo-check): `UserProfile` (global, copies username and email from Auth) contains
  `UserTenantProfile` (a `TenantAggregateRoot` with `GamerTag`, `CustomAvatarUrl`). The only tenant handler
  does not save (U2) and takes the tenant and user ids from the request body (U3).
- Each service has its own database; integration events go through MassTransit with an EF outbox
  (RabbitMQ only for the outbox); `IPartitionedIntegrationEvent` gives a Kafka partition key.

The requirement: **one auth and user-profile model that several services (including non-.NET ones) can use,
with multiple tenants.**

Because User is not running and nothing stores tenant data yet, this is the cheapest moment to settle the model.

## 1. Tenant isolation model

| Option | Isolation | Cost | Fits here? |
|---|---|---|---|
| **Shared database, `TenantId` column, EF global query filter** | logical | lowest; one migration set, one connection pool | **yes, start here** |
| Schema per tenant | stronger | migrations run per schema, EF model caching per schema, many schemas to operate | only if a tenant demands it |
| Database per tenant | strongest | per-tenant connection strings, provisioning, migrations fan-out | only for compliance-driven tenants |

**Recommendation:** shared database with a `tenant_id` discriminator and EF Core global query filters, applied
**by convention** (every entity that implements `ITenantScoped`, no per-entity code) from a shared base `DbContext`
in `Shared.Infrastructure`. Keep the tenant lookup behind `ITenantContext` so a later move of one big tenant to its
own database only changes how the connection is resolved. Add PostgreSQL row-level security as a second layer
once the filters work (optional, phase 2 of the rollout), because filters protect only code paths that go through EF.

Trade-off: a missed filter leaks data across tenants. Mitigation is in section 4 (convention, interceptor, tests).

## 2. Identity vs membership

**Recommendation:** the account is **global** and membership is **per tenant**.

- Auth owns: `Account` (unchanged, global login identity), `Tenant` (id, slug, name, status) and
  `TenantMembership` (account id, tenant id, status, roles in that tenant).
- Roles split in two: **platform roles** (`PlatformAdmin`, formerly `SuperAdmin`) live on the account;
  **tenant roles** (`TenantOwner`, `TenantAdmin`, `Member`, ...) live on the membership. Today's
  `Admin` / `User` map to tenant roles.
- Username and email stay **globally unique** (one login, many tenants). Per-tenant display data
  (`GamerTag`, avatar) is not an identity: it lives in User.
- User service: keep `UserProfile` as the global profile and keep `UserTenantProfile`, but **rework it**: it is a
  per-tenant profile owned by User, **not** a child of the `UserProfile` aggregate (U7). It is looked up by
  `(tenant_id, user_id)`, and is created when a membership is created (event from Auth, see section 4).
  The `TenantAggregateRoot` base is kept; the `UserProfile.TenantProfiles` collection and the
  `UserTenantProfile.UserProfile` navigation are dropped.

Trade-off: a per-tenant account (a separate login per tenant) is simpler for isolation but forces users to keep
many passwords and makes "one profile shared by several services" impossible; it is rejected.

## 3. Tenant resolution and switching

**Recommendation:** the **access token is the only source of truth**: it carries one `tenant_id` claim (the active
tenant) next to the roles for that tenant.

- Login returns a token for the account's **default tenant** (or the only one). If the account has several, the
  response lists them and the client calls `POST /api/v1/auth/switch-tenant { tenantId }`.
- **Switching** checks the membership and **re-issues** an access token (and rotates the refresh token, which
  stays bound to the account, not to a tenant). No state lives in the service.
- A request header (`X-Tenant-Id`) is **not trusted** by services. It may be used for the pre-token calls
  (login, switch) and, if present with a token, must equal the claim or the request is rejected (403).
- A subdomain (`acme.example.com`) is resolved **at the gateway only**: it can pre-select the tenant for login and
  can reject a token whose claim does not match. Services never read the host.
- Service-to-service calls and consumers use a **system** context (section 4), never a user-supplied id.

Trade-off: one active tenant per token means two tabs in two tenants need two tokens. Accepted; it is the
common model and avoids a very large claim.

## 4. Propagation (so a service cannot forget it)

In `OmniCore.Shared`:

- `ITenantContext { TenantId? TenantId; bool IsSystem; }`. The HTTP implementation reads the claim from
  `ICurrentUserService`; consumers and jobs get a scoped implementation set from the message or explicitly
  as system.
- **MediatR behaviour** `TenantBehavior`: a request that implements `ITenantScopedRequest` fails with 403 when there
  is no tenant. Handlers never receive a tenant id as an argument from a client: ids from the request body are
  removed (this fixes U3).
- **EF**: a shared `TenantAwareDbContext` applies `HasQueryFilter(e => e.TenantId == ctx.TenantId)` to every
  `ITenantScoped` entity by convention, and a `SaveChanges` interceptor **stamps** `TenantId` on added entities and
  **rejects** a change whose `TenantId` differs from the context. `IgnoreQueryFilters()` is allowed only inside an
  explicit `using (tenantContext.BeginSystemScope())`, which is logged.
- **Events**: integration events that concern a tenant implement `ITenantIntegrationEvent { TenantId }`; the
  partition key becomes `{tenantId}:{aggregateId}` so one tenant's events for one aggregate stay ordered.
  The outbox row keeps the tenant. Consumers set the tenant context from the event before handling.
- **Tests**: an architecture test that fails when an `ITenantScoped` entity is configured without the filter, and
  a cross-tenant read test per service.

## 5. Authorization and token verification

- A role claim means "role **in the active tenant**". Platform roles use a separate claim (`platform_roles`).
  Policies are written against these (`RequireTenantRole`, `RequirePlatformRole`); fine-grained permissions can be
  added as a `permissions` claim later without changing the model.
- A platform admin acting inside a tenant they are not a member of goes through an explicit,
  **audited** "act as" scope, not through a silent bypass.
- **Verification by other services, including non-.NET ones:** move from HS256 with a shared secret to an
  **asymmetric** algorithm (RS256 or ES256). Auth publishes a **JWKS** document
  (`/.well-known/jwks.json`) with a `kid`; every other service validates with the public key only, and a key
  rotation is a JWKS change. This is roadmap phase 4 and is a **prerequisite** for adding non-.NET services:
  sharing the HS256 secret with more languages and teams is the weakest part of today's design.
- The token format changes (new claims, new algorithm). That is a breaking change and follows the migration plan
  below, with a transition window.

## 6. Migration plan for existing single-tenant data

Order of change: **Shared -> Auth -> User -> gateway**, each step deployable on its own.

1. **Shared**: add `ITenantContext`, the behaviour, the base `DbContext` and interceptor, `ITenantIntegrationEvent`.
   No behaviour change for existing services until they opt in. Tests first.
2. **Auth**: migration adds `tenants` and `tenant_memberships`; seed one **default tenant** with a fixed id;
   backfill a membership for every account (`User` -> `Member`, `Admin` -> `TenantAdmin`,
   `SuperAdmin` -> `Member` plus the platform role). Issue `tenant_id` and `platform_roles` claims. Keep
   accepting tokens **without** the claim during a window and treat them as the default tenant. Add
   `switch-tenant`. Solve A15 (first admin) here.
3. **User**: finish the service (U1) on the new model: `tenant_id` columns added nullable, backfilled with the
   default tenant, then `NOT NULL`, filters on. Create the per-tenant profile from the membership event.
4. **Gateway**: route User, add the tenant pre-selection (subdomain/header) and per-tenant rate limiting; end the
   transition window and reject tokens without `tenant_id`.
5. Asymmetric signing and JWKS (phase 4) land between steps 2 and 4 so the new claims ship on the new token format
   once instead of twice.

Every EF migration is kept in sync with the entity change in the same commit. Existing data needs no export:
the default tenant absorbs it.

## 7. Risks and open questions for the owner

1. **Tenant id type.** The existing `TenantId` wraps a string. I recommend a `Guid` (stored as `uuid`) plus a unique
   human slug, because a string id invites typos and case bugs. Changing it now is cheap (only User uses it).
2. **Who creates tenants and how people join?** Self-service sign-up, admin-created only, or invitations?
   This decides the endpoints and the first-admin story.
3. **Global or per-tenant uniqueness** of username and email. I recommend global (one login). Say if a tenant must
   be able to reuse a username.
4. **Do you need a hard isolation guarantee for any tenant** (compliance, data residency)? If yes, plan
   database-per-tenant for those tenants from the start; if not, shared database plus filters (and RLS later) is enough.
5. **Is the `GamerTag` per-tenant profile the right shape?** It reads like a game or community product. The ADR keeps
   the concept; tell me what else a tenant profile must hold.
6. **Platform admin cross-tenant access:** allowed, and with what audit trail?
7. **Refresh token scope:** bound to the account (recommended) or to one tenant? It changes `switch-tenant` and logout.
8. **Cache and events:** Redis keys and Kafka partitions must include the tenant. The cache code is only partly
   used today (S6), so this is fixed when it is revived.
9. **Risk: a missed filter leaks data.** Mitigated by convention, interceptor, an architecture test and, later, RLS,
   but it is the main risk of the shared-database model.
10. **Risk: breaking token change.** Needs the transition window above and a coordinated deploy of Auth and every
    consumer of the token. Today only Auth and the (non-running) User service exist, so the blast radius is small now
    and grows with every service added: another reason to decide before adding more.
11. **Scope creep:** this ADR does not cover billing, tenant-level settings or per-tenant branding.

## Decision

Accepted 2026-11-24 by delegation, with the recommendations above and these answers to section 7:

1. **Tenant id:** `Guid` (uuid) plus a unique slug. `TenantId` in Shared.Domain was changed (only the unused `UserTenantProfile` referenced it).
2. **Who creates tenants:** admin-created only (platform admin); people join when an admin adds a membership. Self-service sign-up and invitations are out of scope until a real need. A seeded **default tenant** absorbs all existing data, so a single-user install behaves as today.
3. **Uniqueness:** username and e-mail stay global.
4. **Hard isolation:** not required. Shared database, `tenant_id` column, EF query filters by convention, RLS later and optional.
5. **Tenant profile shape:** keep `UserTenantProfile` (GamerTag, avatar) as the per-tenant profile, reworked per section 2; extend when a real use appears.
6. **Platform admin in a tenant:** only through an explicit, logged "act as" scope; not built before it is needed.
7. **Refresh token:** bound to the account; `switch-tenant` re-issues only the access token.
8. **Cache keys and event partitions** include the tenant (`TenantPartitionKey`); cache is fixed when revived (S6).

Implementation follows section 6, one CI-verified step at a time on `develop` (no separate branch): 1 Shared, 2 Auth (with the claim on the RS256 tokens), 3 User, 4 gateway and end of the transition window. **No fourth service starts before steps 1-3 are green**, so it is born tenant-aware.
