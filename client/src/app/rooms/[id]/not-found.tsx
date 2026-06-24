import Link from "next/link";

export default function RoomNotFound() {
  return (
    <main className="min-h-screen bg-gray-50 p-8 dark:bg-gray-900">
      <div className="mx-auto max-w-2xl">
        <h1 className="mb-2 text-3xl font-bold text-gray-900 dark:text-gray-100">
          Room Not Found
        </h1>
        <p className="mb-6 text-sm text-gray-500 dark:text-gray-400">
          That room doesn&apos;t exist or may have been removed.
        </p>
        <Link
          href="/rooms"
          className="text-sm text-blue-600 underline hover:text-blue-700 dark:text-blue-400 dark:hover:text-blue-300"
        >
          ← Back to all rooms
        </Link>
      </div>
    </main>
  );
}
