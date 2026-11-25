"use client";

import { useState } from "react";
import { Alert } from "@/shared/ui/Alert";
import { Button } from "@/shared/ui/Button";
import { messageOf } from "@/shared/ui/errors";
import { authApi } from "../api";

/** For signed-in users: sends a new verification link to their own address. */
export function RequestVerificationButton() {
  const [state, setState] = useState<"idle" | "busy" | "sent">("idle");
  const [error, setError] = useState<string>();

  async function send() {
    setError(undefined);
    setState("busy");
    try {
      await authApi.requestEmailVerification();
      setState("sent");
    } catch (e) {
      setError(messageOf(e));
      setState("idle");
    }
  }

  return (
    <div className="space-y-2">
      {error ? <Alert>{error}</Alert> : null}
      {state === "sent" ? (
        <p className="text-sm text-zinc-500">A link is on its way. It works once and expires in 24 hours.</p>
      ) : (
        <Button type="button" variant="ghost" busy={state === "busy"} onClick={send}>
          Send verification e-mail
        </Button>
      )}
    </div>
  );
}
