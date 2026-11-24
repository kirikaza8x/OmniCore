"use client";

import Link from "next/link";
import { useState } from "react";
import { Alert } from "@/shared/ui/Alert";
import { Button } from "@/shared/ui/Button";
import { Field } from "@/shared/ui/Field";
import { messageOf } from "@/shared/ui/errors";
import { authApi } from "../api";
import { forgotPasswordSchema } from "../schemas";

export function ForgotPasswordForm() {
  const [error, setError] = useState<string>();
  const [sent, setSent] = useState(false);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(undefined);
    const parsed = forgotPasswordSchema.safeParse(Object.fromEntries(new FormData(event.currentTarget)));
    if (!parsed.success) return setError(parsed.error.issues.map((i) => i.message).join(" "));

    setBusy(true);
    try {
      await authApi.forgotPassword(parsed.data);
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
          If that address belongs to an account, we sent a link to choose a new password. It works once and expires in one hour.
        </p>
        <Link className="text-sm underline" href="/login">Back to sign in</Link>
      </div>
    );
  }

  return (
    <form onSubmit={onSubmit} className="space-y-4" noValidate>
      <h1 className="text-xl font-semibold">Forgot your password?</h1>
      <p className="text-sm text-zinc-500">Enter your e-mail and we will send you a link to choose a new one.</p>
      {error ? <Alert>{error}</Alert> : null}
      <Field label="Email" name="email" type="email" autoComplete="email" required />
      <Button type="submit" busy={busy} className="w-full">
        Send reset link
      </Button>
      <p className="text-sm text-zinc-500">
        <Link className="underline" href="/login">Back to sign in</Link>
      </p>
    </form>
  );
}
