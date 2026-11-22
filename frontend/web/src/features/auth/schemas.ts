import { z } from "zod";

// Mirrors the backend validators (Register / Login). The backend stays the authority; this only saves a round trip.
export const loginSchema = z.object({
  identifier: z.string().trim().min(1, "Username or email is required.").max(256),
  password: z.string().min(1, "Password is required.").max(128),
});

export const registerSchema = z.object({
  username: z.string().trim().min(3, "At least 3 characters.").max(30, "At most 30 characters."),
  email: z.string().trim().max(256).pipe(z.email("A valid email is required.")),
  password: z
    .string()
    .min(8, "At least 8 characters.")
    .max(128)
    .regex(/[A-Z]/, "Add an uppercase letter.")
    .regex(/[a-z]/, "Add a lowercase letter.")
    .regex(/[0-9]/, "Add a number.")
    .regex(/[^A-Za-z0-9]/, "Add a special character."),
});

export type LoginInput = z.infer<typeof loginSchema>;
export type RegisterInput = z.infer<typeof registerSchema>;
