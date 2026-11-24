"use client";

import Link from "next/link";
import { useState } from "react";
import { Alert } from "@/shared/ui/Alert";
import { Button } from "@/shared/ui/Button";
import { Field } from "@/shared/ui/Field";
import { messageOf } from "@/shared/ui/errors";
import { authApi } from "../api";
import { resendVerificationSchema } from "../schemas";

export function ResendVerificationForm() {
  const [error, setError] = useState<string>();
  const [sent, setSent] = useState(false);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(undefined);
    const parsed = resendVerificationSchema.safeParse(Object.fromEntries(new FormData(event.currentTarget)));
    if (!parsed.success) return setError(parsed.error.issues.map((i) => i.message).join(" "));

    setBusy(true);
    try {
      await authApi.resendVerification(parsed.data);
      setSent(true);
    } catch (e) {
      setError(messageOf(e));
    } finally {
      setBusy(false);
    }
  }

  if (sent) {
    // Same text whether or not the address has an account: the backend does not say.
    return (
      <div className="space-y-4">
        <h1 className="text-xl font-semibold">Check your e-mail</h1>
        <p className="text-sm text-zinc-500">
          If that address belongs to an account that is not confirmed yet, we sent a new link. It works once and expires in 24 hours.
        </p>
        <Link className="text-sm underline" href="/login">Back to sign in</Link>
      </div>
    );
  }

  return (
    <form onSubmit={onSubmit} className="space-y-4" noValidate>
      <h1 className="text-xl font-semibold">Confirm your e-mail</h1>
      <p className="text-sm text-zinc-500">Enter the e-mail you registered with and we will send a new confirmation link.</p>
      {error ? <Alert>{error}</Alert> : null}
      <Field label="Email" name="email" type="email" autoComplete="email" required />
      <Button type="submit" busy={busy} className="w-full">
        Send confirmation link
      </Button>
      <p className="text-sm text-zinc-500">
        <Link className="underline" href="/login">Back to sign in</Link>
      </p>
    </form>
  );
}
