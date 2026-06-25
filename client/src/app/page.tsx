import Link from "next/link";

export default function Home() {
  return (    <main className="min-h-screen bg-gray-50 p-8 dark:bg-gray-900">
      <div className="mx-auto max-w-5xl">
        <h1 className="mb-2 text-3xl font-bold text-gray-900 dark:text-gray-100">
          Welcome to ConferenceHub
        </h1>
        <p className="mb-8 text-gray-500 dark:text-gray-400">
          Find and book conference rooms for your team.
        </p>

        <div className="flex gap-4">
          <Link
            href="/rooms"
            className="rounded-lg bg-blue-600 px-6 py-3 text-sm font-medium text-white hover:bg-blue-700"
          >
            Browse Rooms
          </Link>
          <Link
            href="/bookings"
            className="rounded-lg border border-gray-200 bg-white px-6 py-3 text-sm font-medium text-gray-700 hover:bg-gray-50 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-200 dark:hover:bg-gray-700"
          >
            View Bookings
          </Link>
        </div>
      </div>
    </main>
  );
}
