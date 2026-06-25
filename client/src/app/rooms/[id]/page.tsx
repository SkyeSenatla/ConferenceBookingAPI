import { notFound } from "next/navigation";
import Link from "next/link";
import { RoomResponse } from "@/types";
import { BookingForm } from "@/components/BookingForm";
import { QuickBookingForm } from "@/components/QuickBookingForm"; 



const API_URL = process.env.NEXT_PUBLIC_API_URL;

async function getRoom(id: string): Promise<RoomResponse | null> {
  const res = await fetch(`${API_URL}/api/rooms/${id}`, { cache: "no-store" });
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Failed to fetch room: ${res.status}`);
  return res.json();
}

export default async function RoomDetailPage({
  params,
}: {
  params: { id: string };
}) {
  const room = await getRoom(params.id);
  if (!room) notFound();

  return (
    <main className="min-h-screen bg-gray-50 p-8 dark:bg-gray-900">
      <div className="mx-auto max-w-2xl">
        <Link
          href="/rooms"
          className="mb-6 inline-flex items-center text-sm text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
        >
          ← Back to rooms
        </Link>

        <div className="mb-8">
          <div className="mb-2 flex items-center gap-3">
            <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100">
              {room.name}
            </h1>
            <span
              className={`rounded px-2 py-1 text-sm font-medium ${room.isAvailable
                  ? "bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300"
                  : "bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300"
                }`}
            >
              {room.isAvailable ? "Available" : "Unavailable"}
            </span>
          </div>
          <p className="text-gray-500 dark:text-gray-400">
            {room.floor} · Capacity: {room.capacity} people
          </p>
        </div>

        {room.isAvailable ? (
          <div className="space-y-8">
            <div>
              <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-gray-400 
                dark:text-gray-500">
                Full booking form — React Query
              </p>
              <BookingForm roomId={room.id} roomName={room.name} />
            </div>
            <div>
              <p className="mb-2 text-xs font-semibold uppercase tracking-wide text-gray-400 
dark:text-gray-500">
                Quick book — Server Action
              </p>
              <QuickBookingForm roomId={room.id} />
            </div>
          </div>
        ) : (
          <div className="rounded-lg border border-yellow-200 bg-yellow-50 p-4 dark:border-yellow-800 dark:bg-yellow-950">
            <p className="text-sm font-medium text-yellow-800 dark:text-yellow-300">
              This room is currently unavailable for booking.
            </p>
          </div>
        )}
      </div>
    </main>
  );
}
