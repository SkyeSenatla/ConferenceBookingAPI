function RoomCardSkeleton() {
  return (
    <div className="rounded-xl border border-gray-200 p-5 dark:border-gray-700">
      <div className="mb-3 flex items-start justify-between gap-2">
        <div className="h-5 w-2/3 animate-pulse rounded bg-gray-200 dark:bg-gray-700" />
        <div className="h-5 w-16 animate-pulse rounded-full bg-gray-200 dark:bg-gray-700" />
      </div>
      <div className="h-4 w-1/2 animate-pulse rounded bg-gray-200 dark:bg-gray-700" />
    </div>
  );
}
export { RoomCardSkeleton };

function RoomListSkeleton() {
  return (
    <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
      {Array.from({ length: 4 }).map((_, i) => (
        <RoomCardSkeleton key={i} />
      ))}
    </div>
  );
}
export { RoomListSkeleton };