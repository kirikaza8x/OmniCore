"use client";

import { useState } from "react";
import { Alert } from "@/shared/ui/Alert";
import { Button } from "@/shared/ui/Button";
import { Field } from "@/shared/ui/Field";
import { messageOf } from "@/shared/ui/errors";
import { profileApi } from "../api";
import { addressSchema } from "../schemas";
import type { UserAddress } from "../types";

export function AddressList({ addresses, onChanged }: { addresses: UserAddress[]; onChanged: () => void }) {
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);

  async function onAdd(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(undefined);
    const form = event.currentTarget;
    const data = new FormData(form);
    const parsed = addressSchema.safeParse({ ...Object.fromEntries(data), isPrimary: data.get("isPrimary") === "on" });
    if (!parsed.success) return setError(parsed.error.issues.map((i) => i.message).join(" "));

    setBusy(true);
    try {
      await profileApi.addAddress(parsed.data);
      form.reset();
      onChanged();
    } catch (e) {
      setError(messageOf(e));
    } finally {
      setBusy(false);
    }
  }

  async function onRemove(id: string) {
    setError(undefined);
    try {
      await profileApi.removeAddress(id);
      onChanged();
    } catch (e) {
      setError(messageOf(e));
    }
  }

  return (
    <section className="space-y-4">
      <h2 className="text-lg font-semibold">Addresses ({addresses.length}/10)</h2>
      {error ? <Alert>{error}</Alert> : null}
      <ul className="space-y-2">
        {addresses.map((a) => (
          <li key={a.id} className="flex items-start justify-between rounded-md border border-zinc-200 p-3 text-sm dark:border-zinc-800">
            <span>
              {a.addressLine1}{a.addressLine2 ? `, ${a.addressLine2}` : ""}, {a.city}{a.state ? `, ${a.state}` : ""}, {a.country} {a.zipCode}
              {a.isPrimary ? <strong className="ml-2 text-indigo-600">Primary</strong> : null}
            </span>
            <Button variant="ghost" onClick={() => onRemove(a.id)}>Remove</Button>
          </li>
        ))}
      </ul>
      <form onSubmit={onAdd} className="grid gap-3 sm:grid-cols-2">
        <Field label="Address" name="addressLine1" required />
        <Field label="Address line 2" name="addressLine2" />
        <Field label="City" name="city" required />
        <Field label="State" name="state" />
        <Field label="Country" name="country" required />
        <Field label="Zip code" name="zipCode" />
        <label className="flex items-center gap-2 text-sm sm:col-span-2">
          <input type="checkbox" name="isPrimary" /> Make primary
        </label>
        <div className="sm:col-span-2"><Button type="submit" busy={busy}>Add address</Button></div>
      </form>
    </section>
  );
}
