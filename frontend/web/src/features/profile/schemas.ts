import { z } from "zod";

// Limits match the User domain (names 100, bio 500, phone 32, theme list, language format).
export const profileSchema = z.object({
  firstName: z.string().trim().max(100).optional(),
  lastName: z.string().trim().max(100).optional(),
  bio: z.string().trim().max(500, "At most 500 characters.").optional(),
  phoneNumber: z.string().trim().max(32).optional(),
});

export const settingsSchema = z.object({
  theme: z.enum(["Light", "Dark", "System"]),
  language: z.string().regex(/^[a-z]{2,3}(-[A-Za-z0-9]{2,8})?$/, "Use a code like en or vi-VN."),
  emailNotificationsEnabled: z.boolean(),
  pushNotificationsEnabled: z.boolean(),
});

export const addressSchema = z.object({
  addressLine1: z.string().trim().min(1, "Address is required.").max(200),
  addressLine2: z.string().trim().max(200).optional(),
  city: z.string().trim().min(1, "City is required.").max(100),
  state: z.string().trim().max(100).optional(),
  country: z.string().trim().min(1, "Country is required.").max(100),
  zipCode: z.string().trim().max(20).optional(),
  isPrimary: z.boolean(),
});

export type ProfileInput = z.infer<typeof profileSchema>;
export type SettingsInput = z.infer<typeof settingsSchema>;
export type AddressInput = z.infer<typeof addressSchema>;
