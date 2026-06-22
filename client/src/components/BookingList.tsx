import { BookingResponse, BookingType, PagedResponse } from "@/types";

interface BookingListProps {
  data: PagedResponse<BookingResponse> | undefined;
  isPending: boolean;
  isError: boolean;
  error: Error | null;
  currentPage: number;
  onPageChange: (page: number) => void;
}

const TYPE_COLOURS: Record<BookingType, string> = {
  Meeting: "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300",
  ClientPresentation:
    "bg-purple-100 text-purple-700 dark:bg-purple-900 dark:text-purple-300",
  Training: "bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300",
  Maintenance:
    "bg-yellow-100 text-yellow-700 dark:bg-yellow-900 dark:text-yellow-300",
  TeamEvent:
    "bg-orange-100 text-orange-700 dark:bg-orange-900 dark:text-orange-300",
};

function TypeBadge({ type }: { type: BookingType }) {
  return (
    <span
      className={`rounded px-1.5 py-0.5 text-xs font-medium ${TYPE_COLOURS[type] ?? "bg-gray-100 text-gray-700"}`}
    >
      {type}
    </span>
  );
}

function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString(undefined, {
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function BookingList({
  data,
  isPending,
  isError,
  error,
  currentPage,
  onPageChange,
}: BookingListProps) {
  if (isPending) {
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

  if (isError) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-4 dark:border-red-800 dark:bg-red-950">
        <p className="text-sm font-medium text-red-700 dark:text-red-400">
          Could not load bookings. {error?.message}
        </p>
      </div>
    );
  }

  if (!data || data.data.length === 0) {
    return (
      <p className="text-sm text-gray-500 dark:text-gray-400">
        No bookings found.
      </p>
    );
  }

  return (
    <div>
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-gray-200 dark:border-gray-700">
              {["Title", "Room", "Type", "Start", "End", "Organiser"].map(
                (h) => (
                  <th
                    key={h}
                    className="py-2 pr-4 text-left text-xs font-medium uppercase tracking-wide text-gray-500 dark:text-gray-400"
                  >
                    {h}
                  </th>
                )
              )}
            </tr>
          </thead>
          <tbody>
            {data.data.map((b) => (
              <tr
                key={b.id}
                className="border-b border-gray-100 hover:bg-gray-50 dark:border-gray-800 dark:hover:bg-gray-800"
              >
                <td className="py-2 pr-4 font-medium text-gray-900 dark:text-gray-100">
                  {b.title}
                </td>
                <td className="py-2 pr-4 text-gray-600 dark:text-gray-400">
                  {b.roomName}{" "}
                  <span className="text-xs text-gray-400">({b.floor})</span>
                </td>
                <td className="py-2 pr-4">
                  <TypeBadge type={b.type} />
                </td>
                <td className="py-2 pr-4 text-gray-600 dark:text-gray-400">
                  {formatDateTime(b.startTime)}
                </td>
                <td className="py-2 pr-4 text-gray-600 dark:text-gray-400">
                  {formatDateTime(b.endTime)}
                </td>
                <td className="py-2 text-gray-600 dark:text-gray-400">
                  {b.organizerEmail}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="mt-4 flex items-center justify-between text-sm text-gray-500 dark:text-gray-400">
        <span>{data.totalCount} booking{data.totalCount !== 1 ? "s" : ""} total</span>
        <div className="flex items-center gap-2">
          <button
            disabled={!data.hasPreviousPage}
            onClick={() => onPageChange(currentPage - 1)}
            className="rounded border border-gray-200 px-3 py-1 hover:bg-gray-100 disabled:opacity-40 dark:border-gray-700 dark:hover:bg-gray-800"
          >
            Previous
          </button>
          <span>
            Page {data.page} of {data.totalPages}
          </span>
          <button
            disabled={!data.hasNextPage}
            onClick={() => onPageChange(currentPage + 1)}
            className="rounded border border-gray-200 px-3 py-1 hover:bg-gray-100 disabled:opacity-40 dark:border-gray-700 dark:hover:bg-gray-800"
          >
            Next
          </button>
        </div>
      </div>
    </div>
  );
}
