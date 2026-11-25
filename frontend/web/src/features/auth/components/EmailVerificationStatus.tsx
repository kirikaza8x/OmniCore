"use client";

import { useEffect, useState } from "react";
import { authApi } from "../api";
import type { CurrentAccount } from "../types";
import { RequestVerificationButton } from "./RequestVerificationButton";

/** Shows whether the signed-in user's address is verified; offers to send the link when it is not. */
export function EmailVerificationStatus() {
  const [account, setAccount] = useState<CurrentAccount | null | undefined>(undefined);

  useEffect(() => {
    let alive = true;
    authApi
      .me()
      .then((a) => alive && setAccount(a))
      .catch(() => alive && setAccount(null));
    return () => {
      alive = false;
    };
  }, []);

  if (account === undefined) return <p className="text-sm text-zinc-500">Checking…</p>;
  // Could not load the status: still allow asking for a link (the backend does nothing if it is already verified).
  if (account === null) return <RequestVerificationButton />;
  if (!account.email) return <p className="text-sm text-zinc-500">This account has no e-mail address.</p>;
  if (account.isEmailConfirmed) return <p className="text-sm text-green-700 dark:text-green-400">{account.email} is verified.</p>;

  return (
    <div className="space-y-2">
      <p className="text-sm text-zinc-500">{account.email} is not verified yet.</p>
      <RequestVerificationButton />
    </div>
  );
}
