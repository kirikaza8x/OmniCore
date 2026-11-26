"use client";

import { useState } from "react";
import { Alert } from "@/shared/ui/Alert";
import { Button } from "@/shared/ui/Button";
import { Field } from "@/shared/ui/Field";
import { messageOf } from "@/shared/ui/errors";
import { profileApi } from "../api";
import { settingsSchema } from "../schemas";
import { applyTheme } from "../theme";
import type { UserSettings } from "../types";

export function SettingsForm({ settings, onSaved }: { settings: UserSettings; onSaved: () => void }) {
  const [message, setMessage] = useState<{ tone: "error" | "success"; text: string }>();
  const [busy, setBusy] = useState(false);

  async function onSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMessage(undefined);
    const form = new FormData(event.currentTarget);
    const parsed = settingsSchema.safeParse({
      theme: form.get("theme"),
      language: form.get("language"),
      emailNotificationsEnabled: form.get("emailNotificationsEnabled") === "on",
      pushNotificationsEnabled: form.get("pushNotificationsEnabled") === "on",
    });
    if (!parsed.success) return setMessage({ tone: "error", text: parsed.error.issues.map((i) => i.message).join(" ") });

    setBusy(true);
    try {
      await profileApi.updateSettings(parsed.data);
      applyTheme(parsed.data.theme); // takes effect at once, no reload
      setMessage({ tone: "success", text: "Settings saved." });
      onSaved();
    } catch (e) {
      setMessage({ tone: "error", text: messageOf(e) });
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={onSubmit} className="space-y-4">
      <h2 className="text-lg font-semibold">Settings</h2>
      {message ? <Alert tone={message.tone}>{message.text}</Alert> : null}
      <div className="space-y-1">
        <label htmlFor="theme" className="block text-sm font-medium">Theme</label>
        <select id="theme" name="theme" defaultValue={settings.theme}
          className="w-full rounded-md border border-zinc-300 bg-transparent px-3 py-2 text-sm dark:border-zinc-700">
          <option>Light</option>
          <option>Dark</option>
          <option>System</option>
        </select>
      </div>
      <Field label="Language" name="language" defaultValue={settings.language} />
      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" name="emailNotificationsEnabled" defaultChecked={settings.emailNotificationsEnabled} /> Email notifications
      </label>
      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" name="pushNotificationsEnabled" defaultChecked={settings.pushNotificationsEnabled} /> Push notifications
      </label>
      <Button type="submit" busy={busy}>Save settings</Button>
    </form>
  );
}
