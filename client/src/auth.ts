import NextAuth from "next-auth";
import Credentials from "next-auth/providers/credentials";


export const { handlers, auth, signIn, signOut } = NextAuth({
  providers: [
    Credentials({
      credentials: {
        username: { label: "Username", type: "text" },
        password: { label: "Password", type: "password" },
      },
      async authorize(credentials) {
        if (!credentials?.username || !credentials?.password) return null;
        // Step 1 — Exchange credentials for a JWT from the ASP.NET Core backend.
        const loginRes = await fetch(
          `${process.env.NEXT_PUBLIC_API_URL}/api/auth/login`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              username: credentials.username,
              password: credentials.password,
            }),
          }
        );
        // A non-OK response means wrong credentials — return null to reject.
        if (!loginRes.ok) return null;
        const { token } = await loginRes.json();
        // Step 2 — Fetch user profile from /api/auth/me.
        const meRes = await fetch(
          `${process.env.NEXT_PUBLIC_API_URL}/api/auth/me`,
          { headers: { Authorization: `Bearer ${token}` } }
        );
        if (!meRes.ok) return null;
        const { username, role } = await meRes.json();
        return { id: username, name: username, role, backendToken: token };
      },
    }),
  ],
  session: { strategy: "jwt" },
  pages: { signIn: "/login" },
  callbacks: {
    async jwt({ token, user }) {
      if (user) {
        token.role = (user as any).role;
        token.backendToken = (user as any).backendToken;
      }
      return token;
    },
    async session({ session, token }) {
      session.user.role = token.role as string;
      session.user.backendToken = token.backendToken as string;
      return session;
    },
  },
})