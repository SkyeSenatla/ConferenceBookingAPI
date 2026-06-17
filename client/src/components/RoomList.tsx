import { RoomResponse } from "@/types";
import { RoomCard } from "./RoomCard";

interface RoomListProps {
  rooms: RoomResponse[];
  selectedId: string | null;
  onSelect: (id: string) => void;
}

export function RoomList({ rooms, selectedId, onSelect }: RoomListProps) {
  if (rooms.length === 0) {
    return (
      <div className="py-16 text-center">
        <p className="text-lg font-medium text-gray-400">No rooms found.</p>
        <p className="mt-1 text-sm text-gray-400">
          All rooms may be under maintenance.
        </p>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
      {rooms.map((room) => (
        <RoomCard
          key={room.id}
          room={room}
          isSelected={room.id === selectedId}
          onSelect={onSelect}
        />
      ))}
    </div>
  );
}
