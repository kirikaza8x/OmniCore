"use client";

import Link from "next/link";
import { useState } from "react";
import { Alert } from "@/shared/ui/Alert";
import { Button } from "@/shared/ui/Button";
import { messageOf } from "@/shared/ui/errors";
import { authApi } from "../api";

/**
 * The link only opens this page; the token is used when the person presses the button. A mail scanner that opens links
 * in advance therefore cannot burn the single-use token.
 */
export function VerifyEmailPanel({ token }: { token: string }) {
  const [state, setState] = useState<"idle" | "busy" | "done">("idle");
  const [error, setError] = useState<string>();

  async function confirm() {
    setError(undefined);
    setState("busy");
    try {
      await authApi.verifyEmail({ token });
      setState("done");
    } catch (e) {
      setError(messageOf(e));
      setState("idle");
    }
  }

  if (!token) {
    return (
      <div className="space-y-4">
        <h1 className="text-xl font-semibold">Link not valid</h1>
        <p className="text-sm text-zinc-500">This link is incomplete. Sign in and request a new verification e-mail.</p>
        <Link className="text-sm underline" href="/login">Sign in</Link>
      </div>
    );
  }

  if (state === "done") {
    return (
      <div className="space-y-4">
        <h1 className="text-xl font-semibold">E-mail confirmed</h1>
        <p className="text-sm text-zinc-500">Thanks, your address is verified.</p>
        <Link className="text-sm underline" href="/profile">Go to your profile</Link>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <h1 className="text-xl font-semibold">Confirm your e-mail</h1>
      <p className="text-sm text-zinc-500">Press the button to confirm this address for your account.</p>
      {error ? <Alert>{error}</Alert> : null}
      <Button type="button" busy={state === "busy"} onClick={confirm} className="w-full">
        Confirm e-mail
      </Button>
    </div>
  );
}
