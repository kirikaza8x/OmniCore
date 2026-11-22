"use client";

import { Alert } from "@/shared/ui/Alert";
import { messageOf } from "@/shared/ui/errors";
import { useProfile } from "../useProfile";
import { AddressList } from "./AddressList";
import { ProfileForm } from "./ProfileForm";
import { SettingsForm } from "./SettingsForm";

export function ProfilePage() {
  const { profile, loading, error, reload } = useProfile();

  if (loading) return <p className="text-sm text-zinc-500">Loading your profile...</p>;
  if (error || !profile) {
    return (
      <Alert>
        {error ? messageOf(error) : "Profile not found."} If you just registered, your profile is created in the
        background: wait a few seconds and refresh.
      </Alert>
    );
  }

  return (
    <div className="space-y-10">
      <header>
        <h1 className="text-2xl font-semibold">{profile.displayName || profile.username}</h1>
        <p className="text-sm text-zinc-500">@{profile.username}{profile.email ? ` · ${profile.email}` : ""}</p>
      </header>
      <ProfileForm profile={profile} onSaved={reload} />
      <SettingsForm settings={profile.settings} onSaved={reload} />
      <AddressList addresses={profile.addresses} onChanged={reload} />
    </div>
  );
}
