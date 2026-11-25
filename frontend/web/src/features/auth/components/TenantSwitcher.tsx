"use client";

import { useEffect, useState } from "react";
import { authApi } from "../api";
import type { TenantMembership } from "../types";

/** A tenant picker for the nav bar. Shown only when the account belongs to more than one tenant. */
export function TenantSwitcher() {
  const [tenants, setTenants] = useState<TenantMembership[]>([]);
  const [current, setCurrent] = useState<string>("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    let alive = true;
    authApi
      .tenants()
      .then((list) => {
        if (!alive) return;
        setTenants(list);
        setCurrent((list.find((t) => t.isCurrent) ?? list[0])?.id ?? "");
      })
      .catch(() => undefined);
    return () => {
      alive = false;
    };
  }, []);

  if (tenants.length < 2) return null;

  async function onChange(tenantId: string) {
    setBusy(true);
    try {
      await authApi.switchTenant({ tenantId });
      setCurrent(tenantId);
      // Data on the page belongs to the old tenant: load it again.
      window.location.reload();
    } finally {
      setBusy(false);
    }
  }

  return (
    <label className="flex items-center gap-2 text-sm">
      <span className="text-zinc-500">Tenant</span>
      <select
        value={current}
        disabled={busy}
        onChange={(e) => void onChange(e.target.value)}
        className="rounded-md border border-zinc-300 bg-transparent px-2 py-1 dark:border-zinc-700"
      >
        {tenants.map((t) => (
          <option key={t.id} value={t.id}>
            {t.name}
          </option>
        ))}
      </select>
    </label>
  );
}
