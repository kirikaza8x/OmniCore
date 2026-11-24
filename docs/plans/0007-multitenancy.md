# 0007: Multi-tenancy rollout (ADR 0001)

- Status: step 1 written 2026-11-24 on `develop`, unbuilt (CI result awaited); steps 2-4 not started
- Related: ADR 0001, `docs/services-plan.md` guardrail (decide before the next service)

## Steps (each is a CI-green commit before the next)
1. **Shared** (written): `TenantId` as Guid; `ITenantContext` (+ `TenantContext`: claim `tenant_id`, explicit scope for consumers, system scope);
   `ITenantScopedRequest` + `TenantBehavior` (403 `Tenant.Required`); `ITenantIntegrationEvent` + `TenantPartitionKey`;
   EF: `TenantIdConverter`, `ApplyTenantQueryFilters` (by convention), `TenantSaveChangesInterceptor` (stamp / refuse other tenant). 9 tests in `TenancyTests`.
   Nothing changes for existing services: no entity is tenant-scoped yet, no request implements the marker.
2. **Auth**: migration `tenants` + `tenant_memberships`, default tenant with a fixed id, backfill memberships, `tenant_id` and `platform_roles` claims
   (accept tokens without the claim as the default tenant during the window), `POST /auth/switch-tenant`, admin endpoints to create a tenant and add a member.
3. **User**: `tenant_id` on tenant-scoped data, rework `UserTenantProfile` (not a child of `UserProfile`), create it from the membership event, filters on, cross-tenant read test.
4. **Gateway / web**: tenant pre-selection, end the transition window (reject tokens without `tenant_id`), web tenant switcher only if more than one membership.

## Not decided here
RLS, act-as audit trail, invitations, per-tenant settings (ADR 0001 section 7, items 6 and 11).
