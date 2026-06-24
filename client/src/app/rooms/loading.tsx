export default function Loading() {
  return (
    <main className="min-h-screen bg-gray-50 p-8 dark:bg-gray-900">
      <div className="mx-auto max-w-5xl">
        <div className="mb-2 h-9 w-64 animate-pulse rounded bg-gray-200 dark:bg-gray-700" />
        <div className="mb-6 h-4 w-32 animate-pulse rounded bg-gray-200 dark:bg-gray-700" />

        <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <div
              key={i}
              className="h-28 animate-pulse rounded-xl bg-gray-200 dark:bg-gray-700"
            />
          ))}
        </div>
      </div>
    </main>
  );
}
