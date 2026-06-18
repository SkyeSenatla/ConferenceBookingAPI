"use client";
import { useState, useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { RoomList } from "@/components/RoomList";
import { fetchRooms } from "@/lib/api";
import { RoomListSkeleton } from "@/components/RoomListSkeleton";


const STORAGE_KEY = "conferencehub:selectedRoomId";
export default function Home() {
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const { data: rooms, isPending, isError, error, refetch } = useQuery({
    queryKey: ["rooms"],
    queryFn: fetchRooms,
  });
  const selectedRoom = rooms?.find((r) => r.id === selectedId) ?? null;
  // Restore from sessionStorage on mount.
  useEffect(() => {
    const stored = sessionStorage.getItem(STORAGE_KEY);
    if (stored) setSelectedId(stored);
  }, []);
  // Persist to sessionStorage whenever selectedId changes.
  useEffect(() => {
    if (selectedId !== null) {
      sessionStorage.setItem(STORAGE_KEY, selectedId);
    } else {
      sessionStorage.removeItem(STORAGE_KEY);
    }
  },
   [selectedId]);


  return (
    <main className="min-h-screen bg-gray-50 p-8 dark:bg-gray-900">
      <div className="mx-auto max-w-5xl">
        <h1 className="mb-2 text-3xl font-bold text-gray-900 dark:text-gray-100">
          Conference Rooms
        </h1>
        <p className="mb-6 text-sm text-gray-500 dark:text-gray-400">
          {isPending ? "Loading…" : `${rooms?.length ?? 0} rooms total`}
        </p>
        {selectedRoom && (
          <div className="mb-6 rounded-lg border border-blue-200 bg-blue-50 p-4 dark:border-blue-800 dark:bg-blue-950">
            <p className="text-sm font-medium text-blue-800 dark:text-blue-300">
              Selected: {selectedRoom.name} — {selectedRoom.floor},{" "}
              {selectedRoom.capacity} seats
            </p>
          </div>
        )}
        {isPending && <RoomListSkeleton />}
        {isError && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-4 dark:border-red-800 dark:bg-red-950">
            <p className="text-sm font-medium text-red-700 dark:text-red-400">
              Could not load rooms. {error.message}
            </p>
            <button
              onClick={() => refetch()}
              className="mt-2 text-sm underline text-red-700 dark:text-red-400"
            >
              Try again
            </button>
          </div>
        )}
        {!isPending && !isError && (
          <RoomList
            rooms={rooms}
            selectedId={selectedId}
            onSelect={(id) => setSelectedId((prev) => (prev === id ? null : id))}
          />
        )}
      </div>
    </main>
  );
}