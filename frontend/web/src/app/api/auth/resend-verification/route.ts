import { resendVerificationSchema } from "@/features/auth/schemas";
import { handlePublicAction } from "@/features/auth/server/public-action";

export const POST = (request: Request) => handlePublicAction(request, "/api/v1/auth/email/verify/resend", resendVerificationSchema);
