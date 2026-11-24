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

const passwordRules = z
  .string()
  .min(8, "At least 8 characters.")
  .max(128)
  .regex(/[A-Z]/, "Add an uppercase letter.")
  .regex(/[a-z]/, "Add a lowercase letter.")
  .regex(/[0-9]/, "Add a number.")
  .regex(/[^A-Za-z0-9]/, "Add a special character.");

// Password reset and e-mail verification (ADR 0009). The backend answers the same whether or not the e-mail exists.
export const forgotPasswordSchema = z.object({
  email: z.string().trim().max(256).pipe(z.email("A valid email is required.")),
});

export const resetPasswordSchema = z.object({
  token: z.string().min(1, "The link is not valid.").max(128, "The link is not valid."),
  newPassword: passwordRules,
});

/** The form adds a confirmation field that is never sent. */
export const resetPasswordFormSchema = resetPasswordSchema
  .extend({ confirmPassword: z.string() })
  .refine((v) => v.newPassword === v.confirmPassword, { path: ["confirmPassword"], message: "The passwords do not match." });

export const verifyEmailSchema = z.object({
  token: z.string().min(1, "The link is not valid.").max(128, "The link is not valid."),
});

export type LoginInput = z.infer<typeof loginSchema>;
export type RegisterInput = z.infer<typeof registerSchema>;
export type ForgotPasswordInput = z.infer<typeof forgotPasswordSchema>;
export type ResetPasswordInput = z.infer<typeof resetPasswordSchema>;
export type VerifyEmailInput = z.infer<typeof verifyEmailSchema>;
