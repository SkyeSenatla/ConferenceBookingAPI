import { UserResponse } from "@/types";

interface UserListProps {
  users: UserResponse[] | undefined;
  isPending: boolean;
  isError: boolean;
  error: Error | null;
}

const ROLE_COLOURS: Record<string, string> = {
  Admin:
    "bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300",
  Receptionist:
    "bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300",
  FacilitiesManager:
    "bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300",
  Employee:
    "bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300",
};

export function UserList({ users, isPending, isError, error }: UserListProps) {
  if (isPending) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 4 }).map((_, i) => (
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
          Could not load users. {error?.message}
        </p>
      </div>
    );
  }

  if (!users || users.length === 0) {
    return (
      <p className="text-sm text-gray-500 dark:text-gray-400">No users found.</p>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-gray-200 dark:border-gray-700">
            {["Username", "Role"].map((h) => (
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
          {users.map((u) => (
            <tr
              key={u.username}
              className="border-b border-gray-100 dark:border-gray-800"
            >
              <td className="py-2 pr-4 font-medium text-gray-900 dark:text-gray-100">
                {u.username}
              </td>
              <td className="py-2">
                <span
                  className={`rounded px-1.5 py-0.5 text-xs font-medium ${ROLE_COLOURS[u.role] ?? "bg-gray-100 text-gray-700"}`}
                >
                  {u.role}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
