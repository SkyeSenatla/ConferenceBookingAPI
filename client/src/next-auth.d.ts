import type { DefaultSession } from "next-auth";
import "next-auth/jwt";

declare module "next-auth" {
  interface Session {
    user: {
      role: string;
      backendToken: string;
    } & DefaultSession["user"];
  }
  interface User {
    role: string;
    backendToken: string;
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    role: string;
    backendToken: string;
  }
}
