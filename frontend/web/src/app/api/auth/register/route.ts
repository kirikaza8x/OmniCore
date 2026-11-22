import { handleCredentials } from "@/features/auth/server/credentials";
import { registerSchema } from "@/features/auth/schemas";

export const POST = (request: Request) => handleCredentials(request, "/api/v1/auth/register", registerSchema);
