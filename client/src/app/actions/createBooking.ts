"use server";

import { revalidateTag } from "next/cache";

// The return type models every outcome the form needs to handle. 
// null = nothing has been submitted yet (initial state). 
type BookingState =
    | { status: "success"; message: string }
    | { status: "error"; message: string }
    | null;
// prevState is the first argument — React passes the previous return value here. 
// formData is the second — the browser sends the form fields here. 
export async function quickCreateBooking(
    prevState: BookingState,
    formData: FormData
): Promise<BookingState> {
    const title
        = formData.get("title") as string;
    const organizerEmail = formData.get("organizerEmail") as string;
    const roomId
        = formData.get("roomId") as string;
    // Validation runs on the server — nothing has left the browser yet. 
    if (!title || !organizerEmail) {
        return { status: "error", message: "Title and email are required." };
    }
    // Book 1 hour from now for 1 hour — a sensible default for a quick-book form. 
    const now = new Date();
    const startTime = new Date(now.getTime() + 1 * 60 * 60 * 1000).toISOString();
    const endTime = new Date(now.getTime() + 2 * 60 * 60 * 1000).toISOString();
    const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/bookings`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
            title,
            organizerEmail,
            roomId,
            startTime,
            endTime,
            type: "Meeting",
        }),
    });
    if (!res.ok) {
        const problem = await res.json().catch(() => null);
        return {
            status: "error",
            message: problem?.detail ?? problem?.title ?? "Failed to create booking.",
        };
    }
    // This is where Demo 1 and Demo 4 connect. 
    // The rooms fetch was tagged "rooms". Calling revalidateTag here clears it. 
    // The next visitor to /rooms will get fresh availability data from the API. 
    revalidateTag("rooms", "max");
    return { status: "success", message: "Room booked successfully!" };
} 