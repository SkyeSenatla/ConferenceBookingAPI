import { RoomResponse } from "@/types";

interface RoomCardProps {
  room: RoomResponse;
  isSelected: boolean;
  onSelect: (id: string) => void;
}

export function RoomCard({ room, isSelected, onSelect }: RoomCardProps) {
  return (
    <div
      onClick={() => onSelect(room.id)}
      className={`cursor-pointer rounded-xl border bg-white p-5 transition-all duration-150 ${
        isSelected
          ? "border-blue-500 shadow-md ring-2 ring-blue-100"
          : "border-gray-200 hover:border-gray-300 hover:shadow-sm"
      }`}
    >
      <div className="mb-3 flex items-start justify-between gap-2">
        <h2 className="text-lg font-semibold leading-tight text-gray-900">
          {room.name}
        </h2>

        {/* Ternary: always renders, content changes based on availability */}
        <span
          className={`shrink-0 rounded-full px-2.5 py-0.5 text-xs font-medium ${
            room.isAvailable
              ? "bg-green-100 text-green-700"
              : "bg-red-100 text-red-700"
          }`}
        >
          {room.isAvailable ? "Available" : "Booked"}
        </span>
      </div>

      <p className="text-sm text-gray-500">
        {room.floor} · {room.capacity} people
      </p>

      {/* &&: only renders when the room is booked */}
      {!room.isAvailable && (
        <p className="mt-2 text-xs text-red-500">Next slot: 2:00 PM</p>
      )}
    </div>
  );
}
