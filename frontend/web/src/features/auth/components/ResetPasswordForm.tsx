"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { Alert } from "@/shared/ui/Alert";
import { Button } from "@/shared/ui/Button";
import { Field } from "@/shared/ui/Field";
import { messageOf } from "@/shared/ui/errors";
import { authApi } from "../api";
import { resetPasswordFormSchema } from "../schemas";

export function ResetPasswordForm({ token }: { token: string }) {
  const router = useRouter();
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [error, setError] = useState<string>();
  const [done, setDone] = useState(false);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(undefined);
    const parsed = resetPasswordFormSchema.safeParse({ ...Object.fromEntries(new FormData(event.currentTarget)), token });
    if (!parsed.success) {
      const next: Record<string, string> = {};
      for (const issue of parsed.error.issues) next[String(issue.path[0])] ??= issue.message;
      return setErrors(next);
    }
    setErrors({});

    setBusy(true);
    try {
      await authApi.resetPassword({ token, newPassword: parsed.data.newPassword });
      setDone(true);
      setTimeout(() => router.replace("/login"), 2500);
    } catch (e) {
      setError(messageOf(e));
      setBusy(false);
    }
  }

  if (!token) {
    return (
      <div className="space-y-4">
        <h1 className="text-xl font-semibold">Link not valid</h1>
        <p className="text-sm text-zinc-500">This reset link is incomplete. Ask for a new one.</p>
        <Link className="text-sm underline" href="/forgot-password">Send a new link</Link>
      </div>
    );
  }

  if (done) {
    return (
      <div className="space-y-4">
        <h1 className="text-xl font-semibold">Password changed</h1>
        <p className="text-sm text-zinc-500">You were signed out everywhere. Sign in with your new password.</p>
        <Link className="text-sm underline" href="/login">Go to sign in</Link>
      </div>
    );
  }

  return (
    <form onSubmit={onSubmit} className="space-y-4" noValidate>
      <h1 className="text-xl font-semibold">Choose a new password</h1>
      {error ? (
        <Alert>
          {error} <Link className="underline" href="/forgot-password">Send a new link</Link>
        </Alert>
      ) : null}
      <Field label="New password" name="newPassword" type="password" autoComplete="new-password" error={errors.newPassword} required />
      <Field label="Repeat the password" name="confirmPassword" type="password" autoComplete="new-password" error={errors.confirmPassword} required />
      <Button type="submit" busy={busy} className="w-full">
        Change password
      </Button>
    </form>
  );
}
