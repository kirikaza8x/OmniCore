# 0007: Multi-tenancy rollout (ADR 0001)

- Status: step 1 CI green (developer reported 2026-11-24, after fixing CS1061); step 2a CI green (developer: "continue"); step 2b written, unbuilt (CI awaited); 3, 4 not started
- Related: ADR 0001, `docs/services-plan.md` guardrail (decide before the next service)

## Steps (each is a CI-green commit before the next)
1. **Shared** (written): `TenantId` as Guid; `ITenantContext` (+ `TenantContext`: claim `tenant_id`, explicit scope for consumers, system scope);
   `ITenantScopedRequest` + `TenantBehavior` (403 `Tenant.Required`); `ITenantIntegrationEvent` + `TenantPartitionKey`;
   EF: `TenantIdConverter`, `ApplyTenantQueryFilters` (by convention), `TenantSaveChangesInterceptor` (stamp / refuse other tenant). 9 tests in `TenancyTests`.
   Nothing changes for existing services: no entity is tenant-scoped yet, no request implements the marker.
2a. **Auth persistence** (written): `Tenant`, `TenantMembership` (+ `TenantRoles`), EF configs, hand-written migration `20261124120000_add_tenants` (default tenant with fixed id, every account backfilled: Admin -> TenantAdmin, others Member), snapshot edited, 6 domain tests. Nothing reads the tables yet.
2b. **Auth behaviour** (written 2026-11-24, unbuilt, CI awaited): register and the first-admin seeder add a default-tenant membership (Member / TenantOwner); access tokens carry `tenant_id` and `tenant_role` (login: default tenant if member, else oldest; refresh: optional `tenantId` hint, used only if a member); `GET /auth/tenants`, `POST /auth/switch-tenant` (access token only, refresh token untouched); SuperAdmin `POST /auth/admin/tenants`, `PUT /auth/admin/tenants/{id}/members`. 13 application tests. Existing role claims are unchanged (a separate `platform_roles` claim comes when step 4 retires the old role claims). **Tokens without the claim = default tenant** is applied service-side in step 3 (config `Tenancy__AssumeDefaultTenantId`), where the first tenant-scoped data appears. Original plan text: register/admin seeder add a default-tenant membership, `tenant_id` + `platform_roles` claims (tokens without the claim = default tenant during the window), `POST /auth/switch-tenant`, admin create-tenant / add-member endpoints. Original scope: migration `tenants` + `tenant_memberships`, default tenant with a fixed id, backfill memberships, `tenant_id` and `platform_roles` claims
   (accept tokens without the claim as the default tenant during the window), `POST /auth/switch-tenant`, admin endpoints to create a tenant and add a member.
3. **User**: `tenant_id` on tenant-scoped data, rework `UserTenantProfile` (not a child of `UserProfile`), create it from the membership event, filters on, cross-tenant read test.
4. **Gateway / web**: tenant pre-selection, end the transition window (reject tokens without `tenant_id`), web tenant switcher only if more than one membership.

## Not decided here
RLS, act-as audit trail, invitations, per-tenant settings (ADR 0001 section 7, items 6 and 11).
