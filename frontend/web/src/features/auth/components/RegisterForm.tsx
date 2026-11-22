"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { Alert } from "@/shared/ui/Alert";
import { Button } from "@/shared/ui/Button";
import { Field } from "@/shared/ui/Field";
import { messageOf } from "@/shared/ui/errors";
import { authApi } from "../api";
import { registerSchema } from "../schemas";

export function RegisterForm() {
  const router = useRouter();
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(undefined);
    const parsed = registerSchema.safeParse(Object.fromEntries(new FormData(event.currentTarget)));
    if (!parsed.success) {
      const next: Record<string, string> = {};
      for (const issue of parsed.error.issues) next[String(issue.path[0])] ??= issue.message;
      return setErrors(next);
    }
    setErrors({});

    setBusy(true);
    try {
      await authApi.register(parsed.data);
      router.replace("/profile");
      router.refresh();
    } catch (e) {
      setError(messageOf(e));
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-4" noValidate>
      <h1 className="text-xl font-semibold">Create account</h1>
      {error ? <Alert>{error}</Alert> : null}
      <Field label="Username" name="username" autoComplete="username" error={errors.username} required />
      <Field label="Email" name="email" type="email" autoComplete="email" error={errors.email} required />
      <Field label="Password" name="password" type="password" autoComplete="new-password" error={errors.password} required />
      <Button type="submit" busy={busy} className="w-full">
        Create account
      </Button>
      <p className="text-sm text-zinc-500">
        Already registered? <Link className="underline" href="/login">Sign in</Link>
      </p>
    </form>
  );
}
