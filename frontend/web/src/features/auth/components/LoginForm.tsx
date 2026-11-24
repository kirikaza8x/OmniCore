"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { Alert } from "@/shared/ui/Alert";
import { Button } from "@/shared/ui/Button";
import { Field } from "@/shared/ui/Field";
import { messageOf } from "@/shared/ui/errors";
import { ApiError } from "@/shared/api/problem";
import { authApi } from "../api";
import { loginSchema } from "../schemas";

export function LoginForm() {
  const router = useRouter();
  const [error, setError] = useState<string>();
  const [needsVerification, setNeedsVerification] = useState(false);
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(undefined);
    setNeedsVerification(false);
    const parsed = loginSchema.safeParse(Object.fromEntries(new FormData(event.currentTarget)));
    if (!parsed.success) return setError(parsed.error.issues.map((i) => i.message).join(" "));

    setBusy(true);
    try {
      await authApi.login(parsed.data);
      router.replace("/profile");
      router.refresh();
    } catch (e) {
      // 403 on login only ever means "e-mail not verified yet" (ADR 0010), and only after the password was right.
      setNeedsVerification(e instanceof ApiError && e.status === 403);
      setError(messageOf(e));
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-4">
      <h1 className="text-xl font-semibold">Sign in</h1>
      {error ? (
        <Alert>
          {error}{" "}
          {needsVerification ? <Link className="underline" href="/resend-verification">Send a new link</Link> : null}
        </Alert>
      ) : null}
      <Field label="Username or email" name="identifier" autoComplete="username" required />
      <Field label="Password" name="password" type="password" autoComplete="current-password" required />
      <Button type="submit" busy={busy} className="w-full">
        Sign in
      </Button>
      <p className="text-sm text-zinc-500">
        <Link className="underline" href="/forgot-password">Forgot your password?</Link>
      </p>
      <p className="text-sm text-zinc-500">
        No account? <Link className="underline" href="/register">Create one</Link>
      </p>
    </form>
  );
}
