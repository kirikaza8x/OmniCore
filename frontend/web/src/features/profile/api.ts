import { http } from "@/shared/api/http";
import type { AddressInput, ProfileInput, SettingsInput } from "./schemas";
import type { UserAddress, UserProfile, UserSettings } from "./types";

const base = "/api/proxy/users";

export const profileApi = {
  get: () => http<UserProfile>(`${base}/me`),
  update: (input: ProfileInput) => http<UserProfile>(`${base}/me`, { method: "PUT", json: input }),
  updateSettings: (input: SettingsInput) => http<UserSettings>(`${base}/me/settings`, { method: "PUT", json: input }),
  uploadAvatar: (file: File) => {
    const form = new FormData();
    form.append("file", file);
    return http<{ avatarUrl?: string }>(`${base}/me/avatar`, { method: "POST", body: form });
  },
  addAddress: (input: AddressInput) => http<UserAddress>(`${base}/me/addresses`, { method: "POST", json: input }),
  removeAddress: (id: string) => http<void>(`${base}/me/addresses/${id}`, { method: "DELETE" }),
};
