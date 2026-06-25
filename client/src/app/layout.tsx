import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import Link from "next/link";
import { ThemeToggle } from "@/components/ThemeToggle";
import { Providers } from "./providers";
import { auth, signOut } from "@/auth";


const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});
const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});
export const metadata: Metadata = {
  title: "ConferenceHub",
  description: "Conference room booking",
};
export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const session = await auth();
  return (
    <html
      lang="en"
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="flex min-h-full flex-col bg-gray-50 dark:bg-gray-900">
        <header className="border-b border-gray-200 bg-white px-8 py-3 dark:border-gray-700 dark:bg-gray-900">
          <div className="mx-auto flex max-w-5xl items-center justify-between">
            <Link
              href="/"
              className="text-sm font-semibold text-gray-900 dark:text-gray-100"
            >
              ConferenceHub
            </Link>
            <nav className="flex items-center gap-4">
              <Link href="/rooms">Rooms</Link>
              {session && <Link href="/bookings">Bookings</Link>}
              <ThemeToggle />
              {session ? (
                <div className="flex items-center gap-3">
                  <span className="text-sm text-gray-500">
                    {session.user.name}
                    <span className="ml-1 rounded bg-gray-100 px-1.5 py-0.5 text-xs">
                      {session.user.role}
                    </span>
                  </span>
                  <form action={async () => { "use server"; await signOut({ redirectTo: "/" }); }}>
                    <button type="submit">Sign Out</button>
                  </form>
                </div>
              ) : (
                <Link href="/login">Sign In</Link>
              )}
            </nav>
          </div>
        </header>
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}