/** One tenant the signed-in account belongs to (GET /api/v1/auth/tenants). */
export type TenantMembership = { id: string; slug: string; name: string; role: string; isCurrent?: boolean };

/** What GET /api/v1/auth/me returns (the parts the web client uses). */
export type CurrentAccount = {
  id: string;
  email?: string | null;
  username: string;
  isActive: boolean;
  isEmailConfirmed: boolean;
  roles: string[];
};
