import { BookingResponse, PagedResponse } from "@/types";

const API_URL = process.env.NEXT_PUBLIC_API_URL;

async function getBookings(): Promise<PagedResponse<BookingResponse>> {
  const res = await fetch(`${API_URL}/api/bookings?page=1&pageSize=20`, {
    cache: "no-store",
  });
  if (!res.ok) throw new Error(`Failed to fetch bookings: ${res.status}`);
  return res.json();
}

function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString(undefined, {
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export default async function BookingsPage() {
  const { data: bookings, totalCount } = await getBookings();

  return (
    <>
      <h1 className="mb-2 text-3xl font-bold text-gray-900 dark:text-gray-100">
        Bookings
      </h1>
      <p className="mb-6 text-sm text-gray-500 dark:text-gray-400">
        {totalCount} total bookings
      </p>

      {bookings.length === 0 ? (
        <p className="text-sm text-gray-500 dark:text-gray-400">
          No bookings found.
        </p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-200 dark:border-gray-700">
                {["Title", "Room", "Start", "End", "Organiser"].map((h) => (
                  <th
                    key={h}
                    className="py-2 pr-4 text-left text-xs font-medium uppercase tracking-wide text-gray-500 dark:text-gray-400"
                  >
                    {h}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {bookings.map((b) => (
                <tr
                  key={b.id}
                  className="border-b border-gray-100 hover:bg-gray-50 dark:border-gray-800 dark:hover:bg-gray-800"
                >
                  <td className="py-2 pr-4 font-medium text-gray-900 dark:text-gray-100">
                    {b.title}
                  </td>
                  <td className="py-2 pr-4 text-gray-600 dark:text-gray-400">
                    {b.roomName}
                    <span className="ml-1 text-xs text-gray-400">
                      ({b.floor})
                    </span>
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
      )}
    </>
  );
}
