import { auth } from "@/auth";
import { NextResponse } from "next/server";

export default auth((req) => {
  const isLoggedIn = !!req.auth;
  const isOnProtectedRoute = req.nextUrl.pathname.startsWith("/bookings");
  if (isOnProtectedRoute && !isLoggedIn) {
    return NextResponse.redirect(new URL("/login", req.url));
  }
  return NextResponse.next();
});

export const config = {
  matcher: [
    // Run on all paths except Next.js internals and Auth.js own routes.
    "/((?!_next/static|_next/image|favicon.ico|api/auth).*)",
  ],
};
