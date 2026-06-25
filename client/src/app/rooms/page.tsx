import Link from "next/link";
import { RoomResponse } from "@/types";

const API_URL = process.env.NEXT_PUBLIC_API_URL;

async function getRooms(): Promise<RoomResponse[]> {
  const res = await fetch(`${API_URL}/api/rooms`, { next: {tags: ["rooms"]} });
  if (!res.ok) throw new Error(`Failed to fetch rooms: ${res.status}`);
  return res.json();
}

export default async function RoomsPage() {
  const rooms = await getRooms();

  return (
    <main className="min-h-screen bg-gray-50 p-8 dark:bg-gray-900">
      <div className="mx-auto max-w-5xl">
        <h1 className="mb-2 text-3xl font-bold text-gray-900 dark:text-gray-100">
          Conference Rooms
        </h1>
        <p className="mb-6 text-sm text-gray-500 dark:text-gray-400">
          {rooms.length} rooms available
        </p>

        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
          {rooms.map((room) => (
            <Link
              key={room.id}
              href={`/rooms/${room.id}`}
              className="block rounded-xl border border-gray-200 bg-white p-5 transition hover:border-gray-300 hover:shadow-sm dark:border-gray-700 dark:bg-gray-800 dark:hover:border-gray-600"
            >
              <div className="mb-2 flex items-start justify-between">
                <h2 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                  {room.name}
                </h2>
                <span
                  className={`rounded px-2 py-0.5 text-xs font-medium ${
                    room.isAvailable
                      ? "bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300"
                      : "bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300"
                  }`}
                >
                  {room.isAvailable ? "Available" : "Unavailable"}
                </span>
              </div>
              <p className="text-sm text-gray-500 dark:text-gray-400">
                {room.floor} · {room.capacity} people
              </p>
            </Link>
          ))}
        </div>
      </div>
    </main>
  );
}
