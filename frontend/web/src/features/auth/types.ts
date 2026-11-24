/** What GET /api/v1/auth/me returns (the parts the web client uses). */
export type CurrentAccount = {
  id: string;
  email?: string | null;
  username: string;
  isActive: boolean;
  isEmailConfirmed: boolean;
  roles: string[];
};
