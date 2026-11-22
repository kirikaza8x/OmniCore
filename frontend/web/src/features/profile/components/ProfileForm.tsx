"use client";

import { useState } from "react";
import { Alert } from "@/shared/ui/Alert";
import { Button } from "@/shared/ui/Button";
import { Field } from "@/shared/ui/Field";
import { messageOf } from "@/shared/ui/errors";
import { profileApi } from "../api";
import { profileSchema } from "../schemas";
import type { UserProfile } from "../types";

export function ProfileForm({ profile, onSaved }: { profile: UserProfile; onSaved: () => void }) {
  const [message, setMessage] = useState<{ tone: "error" | "success"; text: string }>();
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMessage(undefined);
    const parsed = profileSchema.safeParse(Object.fromEntries(new FormData(event.currentTarget)));
    if (!parsed.success) return setMessage({ tone: "error", text: parsed.error.issues.map((i) => i.message).join(" ") });

    setBusy(true);
    try {
      await profileApi.update(parsed.data);
      setMessage({ tone: "success", text: "Profile saved." });
      onSaved();
    } catch (e) {
      setMessage({ tone: "error", text: messageOf(e) });
    } finally {
      setBusy(false);
    }
  }

  async function onAvatar(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    if (!file) return;
    setMessage(undefined);
    try {
      await profileApi.uploadAvatar(file);
      setMessage({ tone: "success", text: "Avatar updated." });
      onSaved();
    } catch (e) {
      setMessage({ tone: "error", text: messageOf(e) });
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-4">
      <h2 className="text-lg font-semibold">Personal information</h2>
      {message ? <Alert tone={message.tone}>{message.text}</Alert> : null}
      <div className="flex items-center gap-4">
        {profile.avatarUrl ? (
          // eslint-disable-next-line @next/next/no-img-element -- the avatar host is configured at runtime (MinIO / CDN)
          <img src={profile.avatarUrl} alt="Your avatar" className="size-16 rounded-full object-cover" />
        ) : (
          <div className="size-16 rounded-full bg-zinc-200 dark:bg-zinc-800" aria-hidden />
        )}
        <label className="text-sm underline">
          Change avatar (JPEG, PNG or WebP, up to 5 MB)
          <input type="file" accept="image/jpeg,image/png,image/webp" className="sr-only" onChange={onAvatar} />
        </label>
      </div>
      <div className="grid gap-4 sm:grid-cols-2">
        <Field label="First name" name="firstName" defaultValue={profile.firstName ?? ""} maxLength={100} />
        <Field label="Last name" name="lastName" defaultValue={profile.lastName ?? ""} maxLength={100} />
      </div>
      <Field label="Phone" name="phoneNumber" defaultValue={profile.phoneNumber ?? ""} maxLength={32} />
      <Field label="Bio" name="bio" defaultValue={profile.bio ?? ""} maxLength={500} />
      <Button type="submit" busy={busy}>Save</Button>
    </form>
  );
}
