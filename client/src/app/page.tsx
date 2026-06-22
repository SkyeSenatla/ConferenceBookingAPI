"use client";
import { useState, useEffect } from "react";
import { useQuery } from "@tanstack/react-query";
import { RoomList } from "@/components/RoomList";
import { BookingList } from "@/components/BookingList";
import { UserList } from "@/components/UserList";
import { RoomListSkeleton } from "@/components/RoomListSkeleton";
import {
  fetchRooms,
  fetchBookings,
  fetchUsers,
  loginUser,
  fetchCurrentUser,
} from "@/lib/api";

type Tab = "rooms" | "bookings" | "admin";

const ROOM_KEY = "conferencehub:selectedRoomId";
const TOKEN_KEY = "conferencehub:token";

export default function Home() {
  const [activeTab, setActiveTab] = useState<Tab>("rooms");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [bookingPage, setBookingPage] = useState(1);

  // Auth state — token lives in localStorage so it survives page refresh.
  const [token, setToken] = useState<string | null>(null);
  const [currentUser, setCurrentUser] = useState<{
    username: string;
    role: string;
  } | null>(null);

  // Login form state — local to admin tab.
  const [loginUsername, setLoginUsername] = useState("");
  const [loginPassword, setLoginPassword] = useState("");
  const [loginError, setLoginError] = useState<string | null>(null);
  const [loginPending, setLoginPending] = useState(false);

  // ── Queries ──────────────────────────────────────────────────────────────

  const {
    data: rooms,
    isPending: roomsPending,
    isError: roomsError,
    error: roomsErr,
    refetch: refetchRooms,
  } = useQuery({ queryKey: ["rooms"], queryFn: fetchRooms });

  const {
    data: bookingsData,
    isPending: bookingsPending,
    isError: bookingsError,
    error: bookingsErr,
  } = useQuery({
    queryKey: ["bookings", bookingPage],
    queryFn: () => fetchBookings(bookingPage),
    // Only fetch once the bookings tab is opened for the first time.
    enabled: activeTab === "bookings",
  });

  const {
    data: users,
    isPending: usersPending,
    isError: usersError,
    error: usersErr,
  } = useQuery({
    queryKey: ["users", token],
    queryFn: () => fetchUsers(token!),
    // Only run when the admin tab is open, a token exists, and the caller is an Admin.
    enabled:
      activeTab === "admin" && !!token && currentUser?.role === "Admin",
  });

  const selectedRoom = rooms?.find((r) => r.id === selectedId) ?? null;

  // ── Bootstrap: restore persisted room selection and auth token ───────────

  useEffect(() => {
    const storedRoom = sessionStorage.getItem(ROOM_KEY);
    if (storedRoom) setSelectedId(storedRoom);

    const storedToken = localStorage.getItem(TOKEN_KEY);
    if (storedToken) {
      setToken(storedToken);
      fetchCurrentUser(storedToken)
        .then(setCurrentUser)
        .catch(() => {
          // Token is expired or invalid — clear it silently.
          localStorage.removeItem(TOKEN_KEY);
          setToken(null);
        });
    }
  }, []);

  // Persist selected room to sessionStorage.
  useEffect(() => {
    if (selectedId !== null) {
      sessionStorage.setItem(ROOM_KEY, selectedId);
    } else {
      sessionStorage.removeItem(ROOM_KEY);
    }
  }, [selectedId]);

  // ── Auth handlers ─────────────────────────────────────────────────────────

  async function handleLogin(e: { preventDefault(): void }) {
    e.preventDefault();
    setLoginError(null);
    setLoginPending(true);
    try {
      const newToken = await loginUser(loginUsername, loginPassword);
      localStorage.setItem(TOKEN_KEY, newToken);
      setToken(newToken);
      const user = await fetchCurrentUser(newToken);
      setCurrentUser(user);
      setLoginUsername("");
      setLoginPassword("");
    } catch {
      setLoginError("Invalid username or password.");
    } finally {
      setLoginPending(false);
    }
  }

  function handleLogout() {
    localStorage.removeItem(TOKEN_KEY);
    setToken(null);
    setCurrentUser(null);
  }

  // ── Tab bar ───────────────────────────────────────────────────────────────

  const tabs: { key: Tab; label: string }[] = [
    { key: "rooms", label: "Rooms" },
    { key: "bookings", label: "Bookings" },
    { key: "admin", label: "Admin" },
  ];

  return (
    <main className="min-h-screen bg-gray-50 p-8 dark:bg-gray-900">
      <div className="mx-auto max-w-5xl">

        {/* Tab bar */}
        <div className="mb-6 flex gap-1 border-b border-gray-200 dark:border-gray-700">
          {tabs.map((t) => (
            <button
              key={t.key}
              onClick={() => setActiveTab(t.key)}
              className={`px-4 py-2 text-sm font-medium transition-colors ${
                activeTab === t.key
                  ? "border-b-2 border-blue-600 text-blue-600 dark:border-blue-400 dark:text-blue-400"
                  : "text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
              }`}
            >
              {t.label}
            </button>
          ))}
        </div>

        {/* ── Rooms tab ────────────────────────────────────────────────────── */}
        {activeTab === "rooms" && (
          <>
            <h1 className="mb-2 text-3xl font-bold text-gray-900 dark:text-gray-100">
              Conference Rooms
            </h1>
            <p className="mb-6 text-sm text-gray-500 dark:text-gray-400">
              {roomsPending ? "Loading…" : `${rooms?.length ?? 0} rooms total`}
            </p>

            {selectedRoom && (
              <div className="mb-6 rounded-lg border border-blue-200 bg-blue-50 p-4 dark:border-blue-800 dark:bg-blue-950">
                <p className="text-sm font-medium text-blue-800 dark:text-blue-300">
                  Selected: {selectedRoom.name} — {selectedRoom.floor},{" "}
                  {selectedRoom.capacity} seats
                </p>
              </div>
            )}

            {roomsPending && <RoomListSkeleton />}

            {roomsError && (
              <div className="rounded-lg border border-red-200 bg-red-50 p-4 dark:border-red-800 dark:bg-red-950">
                <p className="text-sm font-medium text-red-700 dark:text-red-400">
                  Could not load rooms. {roomsErr?.message}
                </p>
                <button
                  onClick={() => refetchRooms()}
                  className="mt-2 text-sm underline text-red-700 dark:text-red-400"
                >
                  Try again
                </button>
              </div>
            )}

            {!roomsPending && !roomsError && (
              <RoomList
                rooms={rooms}
                selectedId={selectedId}
                onSelect={(id) =>
                  setSelectedId((prev) => (prev === id ? null : id))
                }
              />
            )}
          </>
        )}

        {/* ── Bookings tab ─────────────────────────────────────────────────── */}
        {activeTab === "bookings" && (
          <>
            <h1 className="mb-2 text-3xl font-bold text-gray-900 dark:text-gray-100">
              Bookings
            </h1>
            <p className="mb-6 text-sm text-gray-500 dark:text-gray-400">
              All upcoming and past conference room bookings.
            </p>
            <BookingList
              data={bookingsData}
              isPending={bookingsPending}
              isError={bookingsError}
              error={bookingsErr as Error | null}
              currentPage={bookingPage}
              onPageChange={(p) => setBookingPage(p)}
            />
          </>
        )}

        {/* ── Admin tab ────────────────────────────────────────────────────── */}
        {activeTab === "admin" && (
          <>
            <h1 className="mb-2 text-3xl font-bold text-gray-900 dark:text-gray-100">
              Admin
            </h1>

            {/* Not logged in — show login form */}
            {!token && (
              <form
                onSubmit={handleLogin}
                className="mt-4 w-full max-w-sm space-y-4"
              >
                <p className="text-sm text-gray-500 dark:text-gray-400">
                  Sign in with an Admin account to view registered users.
                </p>
                <div>
                  <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                    Username
                  </label>
                  <input
                    required
                    type="text"
                    value={loginUsername}
                    onChange={(e) => setLoginUsername(e.target.value)}
                    className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-sm font-medium text-gray-700 dark:text-gray-300">
                    Password
                  </label>
                  <input
                    required
                    type="password"
                    value={loginPassword}
                    onChange={(e) => setLoginPassword(e.target.value)}
                    className="w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-100"
                  />
                </div>
                {loginError && (
                  <p className="text-sm text-red-600 dark:text-red-400">
                    {loginError}
                  </p>
                )}
                <button
                  type="submit"
                  disabled={loginPending}
                  className="w-full rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-50"
                >
                  {loginPending ? "Signing in…" : "Sign in"}
                </button>
              </form>
            )}

            {/* Logged in but not Admin */}
            {token && currentUser && currentUser.role !== "Admin" && (
              <div className="mt-4 rounded-lg border border-yellow-200 bg-yellow-50 p-4 dark:border-yellow-800 dark:bg-yellow-950">
                <p className="text-sm font-medium text-yellow-800 dark:text-yellow-300">
                  Signed in as{" "}
                  <span className="font-bold">{currentUser.username}</span> (
                  {currentUser.role}). Admin role required to view users.
                </p>
                <button
                  onClick={handleLogout}
                  className="mt-2 text-sm underline text-yellow-700 dark:text-yellow-400"
                >
                  Sign out
                </button>
              </div>
            )}

            {/* Logged in as Admin — show users table */}
            {token && currentUser?.role === "Admin" && (
              <>
                <div className="mb-6 flex items-center justify-between">
                  <p className="text-sm text-gray-500 dark:text-gray-400">
                    Signed in as{" "}
                    <span className="font-semibold text-gray-700 dark:text-gray-300">
                      {currentUser.username}
                    </span>
                  </p>
                  <button
                    onClick={handleLogout}
                    className="text-sm underline text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200"
                  >
                    Sign out
                  </button>
                </div>

                <h2 className="mb-4 text-xl font-semibold text-gray-900 dark:text-gray-100">
                  Registered Users
                </h2>
                <UserList
                  users={users}
                  isPending={usersPending}
                  isError={usersError}
                  error={usersErr as Error | null}
                />
              </>
            )}
          </>
        )}
      </div>
    </main>
  );
}
