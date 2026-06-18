import { RoomResponse } from "@/types";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL;
export async function fetchRooms(): Promise<RoomResponse[]> {
  const res = await fetch(`${BASE_URL}/api/rooms`);
  if (!res.ok) {
    throw new Error(`Failed to fetch rooms: ${res.status} ${res.statusText}`);
  }
  return res.json();
}