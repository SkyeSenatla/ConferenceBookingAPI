import { RoomResponse, BookingResponse, PagedResponse, UserResponse, CreateBookingRequest } from "@/types";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL;

export async function fetchRooms(): Promise<RoomResponse[]> {
  const res = await fetch(`${BASE_URL}/api/rooms`);
  if (!res.ok) {
    throw new Error(`Failed to fetch rooms: ${res.status} ${res.statusText}`);
  }
  return res.json();
}

// GET /api/bookings — public, paginated.
export async function fetchBookings(
  page = 1,
  pageSize = 20
): Promise<PagedResponse<BookingResponse>> {
  const res = await fetch(
    `${BASE_URL}/api/bookings?page=${page}&pageSize=${pageSize}`
  );
  if (!res.ok) {
    throw new Error(`Failed to fetch bookings: ${res.status} ${res.statusText}`);
  }
  return res.json();
}

// GET /api/users — Admin only. Requires a valid Bearer token with the Admin role.
// The backend returns 403 Forbidden for any other role.
export async function fetchUsers(token: string): Promise<UserResponse[]> {
  const res = await fetch(`${BASE_URL}/api/users`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) {
    throw new Error(`Failed to fetch users: ${res.status} ${res.statusText}`);
  }
  return res.json();
}

// POST /api/auth/login — exchanges credentials for a JWT.
// Returns the token string on success; throws on invalid credentials (401).
export async function loginUser(
  username: string,
  password: string
): Promise<string> {
  const res = await fetch(`${BASE_URL}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, password }),
  });
  if (!res.ok) {
    throw new Error("Invalid credentials");
  }
  const { token } = await res.json();
  return token;
}

// GET /api/auth/me — decodes the stored JWT server-side and returns username + role.
// Used to verify a cached token is still valid and to read the caller's role.
export async function fetchCurrentUser(
  token: string
): Promise<{ username: string; role: string }> {
  const res = await fetch(`${BASE_URL}/api/auth/me`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) {
    throw new Error("Token invalid or expired");
  }
  return res.json();
}

// POST /api/bookings — creates a new booking. Requires a valid Bearer token.
export async function createBooking(
  data: CreateBookingRequest): Promise<BookingResponse> {
  const res = await fetch(`${BASE_URL}/api/bookings`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(data),
  });
  if (!res.ok) {

    // ASP.NET Core returns RFC 7807 Problem Details on 4xx/5xx. 
    // We try to extract a human-readable message before falling back. 
    const problem = await res.json().catch(() => null);
    const message =
      problem?.detail ??
      problem?.title ??
      `Request failed: ${res.status} ${res.statusText}`;
    throw new Error(message);
  }
  return res.json() as Promise<BookingResponse>;
}