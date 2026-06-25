import { Suspense } from "react";
import { BookingTable } from "@/components/BookingTable";

import { auth } from "@/auth";
const API_URL = process.env.NEXT_PUBLIC_API_URL;
async function getRoomCount(): Promise<number> {
  const res = await fetch(`${API_URL}/api/rooms`, {
    next: { tags: ["rooms"] },
  });
  if (!res.ok) return 0;
  const rooms = await res.json();
  return rooms.length;
}

function BookingTableSkeleton() {
  return (
    <div className="space-y-2">
      {Array.from({ length: 5 }).map((_, i) => (
        <div
          key={i}
          className="h-10 animate-pulse rounded bg-gray-200 dark:bg-gray-700"
        />
      ))}
    </div>
  );
}

export default async function BookingsPage() {
  // Only the fast fetch runs here. The page renders as soon as this resolves. 
  const [roomCount, session] = await Promise.all([getRoomCount(), auth()]);

  return (
    <>
      <h1 className="mb-2 text-3xl font-bold text-gray-900 dark:text-gray-100">
        Bookings
      </h1>
      
        {session && (
          <p className="mb-1 text-sm text-gray-500">
            Signed in as{" "}
            <span className="font-medium">{session.user.name}</span>
            <span className="ml-1 rounded bg-gray-100 px-1.5 py-0.5 text-xs">
              {session.user.role}
            </span>
          </p>
        )}
        <div className="mb-6 flex gap-6">
          <p className="text-sm text-gray-500 dark:text-gray-400">
            <span className="font-semibold text-gray-900 dark:text-gray-100">
              {roomCount}
            </span>{" "}
            rooms in the system
          </p>
        </div>

        {/* 
        The Suspense boundary is the key line. 
        Everything above renders immediately. 
        BookingTable is async — it fetches its own data. 
        While it loads, the skeleton is shown. 
        When it resolves, Next.js streams the real table in to replace it. 
      */}
        <Suspense fallback={<BookingTableSkeleton />}>
          <BookingTable />
        </Suspense>
      </>
        );
} 