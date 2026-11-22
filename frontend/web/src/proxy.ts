import { NextResponse, type NextRequest } from "next/server";

// Cheap gate in front of the pages: no session cookie, no private page. The real check is the backend's: every API
// call carries the token and the backend answers 401 when it is not valid.
export function proxy(request: NextRequest) {
  const signedIn = request.cookies.has("omni_rt");
  const { pathname } = request.nextUrl;

  if (!signedIn && pathname.startsWith("/profile")) {
    return NextResponse.redirect(new URL("/login", request.url));
  }
  if (signedIn && (pathname === "/login" || pathname === "/register")) {
    return NextResponse.redirect(new URL("/profile", request.url));
  }
  return NextResponse.next();
}

export const config = { matcher: ["/profile/:path*", "/login", "/register"] };
